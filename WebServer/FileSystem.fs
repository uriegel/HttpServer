namespace WebServer
open System
open System.IO
open Microsoft.Extensions.Logging
open ActivePatterns
open MimeTypes
open Response
open FixedResponses

module FileSystem =
    let configuration = Configuration.current.Force ()
    let pathSeparator = Path.DirectorySeparatorChar
    let webroot = (FileInfo configuration.webroot).FullName
    let (|IsFileSystem|_|) request = 
        let url = Uri.UnescapeDataString request.data.header.path
        let (url, query) = 
            match url with
            | SplitChar '?' (path, query) -> (path, Some query)
            | _ -> (url, None)
                            
        let path = 
            match pathSeparator with
            | '/' -> url
            | _ -> url.Replace ('/', pathSeparator)
        let relativePath = 
            if path.StartsWith pathSeparator then
                path.Substring 1
            else
                path
        let localFile =
            try
                Some <| Path.Combine (webroot, relativePath)
            with 
            | e -> 
                request.categoryLogger.log LogLevel.Trace <| sprintf "Invalid path: %s, %A" path e
                None

        let isTraversal (fileToCheck: string) = 
            try
                let fileInfo = FileInfo fileToCheck
                not (fileInfo.FullName.StartsWith (webroot, StringComparison.InvariantCultureIgnoreCase))
            with
            | _ -> false

        match localFile with
        | Some localFile -> 
            if localFile.Length < webroot.Length || isTraversal localFile then
                let warning = sprintf "POSSIBLE DIRECTORY TRAVERSAL ATTACK DETECTED! Url: %s" localFile
                request.categoryLogger.log LogLevel.Warning warning
                failwith warning
            match localFile with
            | _ when relativePath.Length = 0 -> 
                let path = Path.Combine (webroot, "index.html")
                if File.Exists path then
                    Some <| FileSystemType.File { path = path; query = query }
                else
                    None
            | _ when File.Exists localFile -> Some <| FileSystemType.File { path = localFile; query = query} 
            | _ when Directory.Exists localFile ->
                if not (url.EndsWith "/") then
                    Some <| Redirection (url + "/" + match query with | Some value -> "?" + value | None -> "")
                else
                    let localFile = Path.Combine (localFile, "index.html")
                    if File.Exists localFile then
                        Some <| FileSystemType.File { path = localFile; query = query} 
                    else
                        None
            | _ -> None

        | None -> None

    let serveFileSystem socketSession request fileType = 
        let asyncSendStream (stream: Stream) (contentType: string) lastModified = 
            async {
                let! bytes = stream.AsyncRead <| int stream.Length
                let headers = 
                    [  
                        { key = HeaderKey.StatusOK; value = None }  
                        { key = HeaderKey.ContentType; value = Some (contentType :> obj) }  
                    ] 
                let headers = 
                    match lastModified with
                    | Some value -> { key = HeaderKey.LastModified; value = Some (value :> obj) } :: headers 
                    | None -> headers
                
                let awaiter = 
                    (request, headers, Some bytes)
                    |> tryCompress contentType 
                    |> addContentLength 
                    |> tryAddExpires contentType
                    |> Response.asyncSendBytes
                do! awaiter
                // TODO: HEAD
                // let bytes = 
                //     match request.header.method with
                //     | Method.Head -> None
                //     | _ -> Some bytes
            }

        let asyncSendFile () =
            async {
                let info = FileInfo fileType.path
                let notModified = 
                    match request.data.header.ifModifiedSince with
                    | Some value ->
                        let fileTime = info.LastWriteTime.AddTicks -(info.LastWriteTime.Ticks % (TimeSpan.FromSeconds 1.0).Ticks)
                        let diff = fileTime - value
                        diff <= TimeSpan.FromMilliseconds 0.0 
                    | None -> false

                if notModified then
                    do! asyncSendNotModifed socketSession request
                else
                    let contentType = 
                        match info.Extension.ToLower () with
                        | ".html" 
                        | ".htm" -> "text/html; charset=UTF-8"
                        | ".css" -> "text/css; charset=UTF-8"
                        | ".js" -> "application/javascript; charset=UTF-8"
                        | ".appcache" -> "text/cache-manifest"
                        | _ -> mimeType.[info.Extension] 

                    let lastModified = Some <| info.LastWriteTime.ToUniversalTime ()
                    try
                        use stream = File.OpenRead fileType.path
                        do! asyncSendStream stream contentType lastModified
                    with 
                    | e -> request.categoryLogger.log LogLevel.Warning <| sprintf "Could not send file: %A" e 
            }
        async {
            // TODO: SendRange
            do! asyncSendFile ()
        }
        