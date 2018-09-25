namespace WebServer
open System
open System.IO
open Microsoft.Extensions.Logging
open ActivePatterns
open MimeTypes

module FileSystem =
    let (|IsFileSystem|_|) request = 
        let url = Uri.UnescapeDataString ((request.header HeaderKey.Path) :?> string)
        let (url, query) = 
            match url with
            | SplitChar '?' (path, query) -> (path, Some query)
            | _ -> (url, None)
                            
        //         var alias = Server.Configuration.Aliases.FirstOrDefault(n => url.StartsWith(n.Value));
        // if (alias != null)
        // {
        //     relativePath = localURL.Substring(alias.Value.Length).Replace('/', '\\');

        //     if (alias.IsRooted)
        //         rootDirectory = alias.Path;
        //     else
        //         rootDirectory = Path.Combine(rootDirectory, alias.Path);
        // }

        let path = url.Replace ('/', '\\')
        let relativePath = 
            if path.StartsWith @"\" then
                path.Substring 1
            else
                path
        let localFile =
            try
                Some <| Path.Combine (Configuration.Current.Webroot, relativePath)
            with 
            | e -> 
                request.categoryLogger.log LogLevel.Trace <| sprintf "Invalid path: %s, %A" path e
                None
        match localFile with
        | Some localFile -> 
            // protect for directory traversal attacks
            if localFile.Length < Configuration.Current.Webroot.Length || not (localFile.StartsWith Configuration.Current.Webroot) then
                let warning = sprintf "POSSIBLE DIRECTORY TRAVERSAL ATTACK DETECTED! Url: %s" localFile
                request.categoryLogger.log LogLevel.Warning warning
                failwith warning
            match localFile with
            | _ when relativePath.Length = 0 -> 
                let path = Path.Combine (Configuration.Current.Webroot, "index.html")
                if File.Exists path then
                    let redirection = 
                        match query with
                        | Some value -> "/?" + value
                        | None -> "/"
                    Some <| Redirection redirection
                else
                    None
            | _ when File.Exists localFile -> Some <| FileSystemType.File { Path = localFile; Query = query} 
            | _ when Directory.Exists localFile ->
                if not (url.EndsWith "/") then
                    Some <| Redirection (url + "/" + match query with | Some value -> "?" + value | None -> "")
                else
                    // localFile = Path.Combine(localFile, alias?.DefaultFile ?? "index.html");
                    let localFile = Path.Combine (localFile, "index.html")
                    if File.Exists localFile then
                        Some <| FileSystemType.File { Path = localFile; Query = query} 
                    else
                        None
            | _ -> None

        | None -> None

    let serveFileSystem socketSession request fileType = 
        let sendFile () =
            async {
                let info = FileInfo fileType.Path
                // TODO: if-modified-since        

                let contentType = 
                    match info.Extension.ToLower () with
                    | ".html" 
                    | ".htm" -> "text/html; charset=UTF-8"
                    | ".css" -> "text/css; charset=UTF-8"
                    | ".js" -> "application/javascript; charset=UTF-8"
                    | ".appcache" -> "text/cache-manifest"
                    | _ -> mimeType.[info.Extension] 

                let lastModified = (info.LastWriteTime.ToUniversalTime ()).ToString "r"
                try
                    use stream = File.OpenRead fileType.Path
                    ()
                    // await SendStreamAsync(stream, contentType, lastModified, noCache);
                with 
                | e -> request.categoryLogger.log LogLevel.Warning <| sprintf "Could not send file: %A" e
                ()
            } 
        async {
            // TODO: SendRange
            do! sendFile ()
        }
