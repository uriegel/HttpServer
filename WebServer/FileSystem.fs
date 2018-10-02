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

    let (|IsFileSystem|_|) request = 
        let url = Uri.UnescapeDataString request.header.Path
        let (url, query) = 
            match url with
            | SplitChar '?' (path, query) -> (path, Some query)
            | _ -> (url, None)
                            
        let path = url.Replace ('/', '\\')
        let relativePath = 
            if path.StartsWith @"\" then
                path.Substring 1
            else
                path
        let localFile =
            try
                Some <| Path.Combine (configuration.Webroot, relativePath)
            with 
            | e -> 
                request.categoryLogger.log LogLevel.Trace <| sprintf "Invalid path: %s, %A" path e
                None

        let isTraversal (fileToCheck: string) = 
            try
                let fileInfo = FileInfo fileToCheck
                not (fileInfo.FullName.StartsWith (configuration.Webroot, StringComparison.InvariantCultureIgnoreCase))
            with
            | _ -> false

        match localFile with
        | Some localFile -> 
            if localFile.Length < configuration.Webroot.Length || isTraversal localFile then
                let warning = sprintf "POSSIBLE DIRECTORY TRAVERSAL ATTACK DETECTED! Url: %s" localFile
                request.categoryLogger.log LogLevel.Warning warning
                failwith warning
            match localFile with
            | _ when relativePath.Length = 0 -> 
                let path = Path.Combine (configuration.Webroot, "index.html")
                if File.Exists path then
                    Some <| FileSystemType.File { Path = path; Query = query }
                else
                    None
            | _ when File.Exists localFile -> Some <| FileSystemType.File { Path = localFile; Query = query} 
            | _ when Directory.Exists localFile ->
                if not (url.EndsWith "/") then
                    Some <| Redirection (url + "/" + match query with | Some value -> "?" + value | None -> "")
                else
                    let localFile = Path.Combine (localFile, "index.html")
                    if File.Exists localFile then
                        Some <| FileSystemType.File { Path = localFile; Query = query} 
                    else
                        None
            | _ -> None

        | None -> None

    let serveFileSystem socketSession request fileType = 
        let asyncSendStream (stream: Stream) (contentType: string) lastModified = 
            async {
                let! bytes = stream.AsyncRead <| int stream.Length
                let (bytes, headers) = tryCompress request contentType bytes
                let contentLengthValue = { key = HeaderKey.ContentLength; value = Some (bytes.Length :> obj) }
                let headers = 
                    match lastModified with
                    | Some value -> headers |> Array.append [|contentLengthValue; { key = HeaderKey.LastModified; value = Some (value :> obj) }|]
                    | None -> headers |> Array.append [|contentLengthValue|]

                do! Response.asyncSend request contentType bytes headers
            }

        let asyncSendFile () =
            async {
                let info = FileInfo fileType.Path
                let notModified = 
                    match request.header.IfModifiedSince with
                    | Some value ->
                        let fileTime = info.LastWriteTime.AddTicks -(info.LastWriteTime.Ticks % (TimeSpan.FromSeconds 1.0).Ticks)
                        let diff = fileTime - request.header.IfModifiedSince.Value
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
                        use stream = File.OpenRead fileType.Path
                        do! asyncSendStream stream contentType lastModified
                    with 
                    | e -> request.categoryLogger.log LogLevel.Warning <| sprintf "Could not send file: %A" e
            } 
        async {
            // TODO: SendRange
            do! asyncSendFile ()
        }
