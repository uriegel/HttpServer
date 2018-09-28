namespace WebServer
open System
open System.IO
open Microsoft.Extensions.Logging
open ActivePatterns
open MimeTypes
open System.IO.Compression

module FileSystem =
    let (|IsFileSystem|_|) request = 
        let url = Uri.UnescapeDataString request.header.Path
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
                    // let redirection = 
                    // match query with
                    // | Some value -> "/?" + value
                    // | None -> "/"
                    Some <| FileSystemType.File { Path = path; Query = query }
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
        let asyncSendStream (stream: Stream) (contentType: string) lastModified = 
            async {
                let! bytes = stream.AsyncRead <| int stream.Length

                let compress (stream: Stream) = 
                    stream.Read (bytes, 0, bytes.Length)

                let compress =  
                    contentType.StartsWith ("application/javascript", StringComparison.CurrentCultureIgnoreCase)
                    || contentType.StartsWith ("text/", StringComparison.CurrentCultureIgnoreCase)
                
                let headers = 
                    [|  
                        { key = HeaderKey.StatusOK; value = None }  
                        { key = HeaderKey.ContentType; value = Some (contentType :> obj) }  
                    |] 

                let compressStream (streamCompressor: Stream->Stream) compressionMethod =
                    use ms = new MemoryStream ()
                    use compressedStream = streamCompressor ms
                    compressedStream.Write (bytes, 0, bytes.Length) |> ignore
                    compressedStream.Close ()
                    ms.Capacity <- int ms.Length
                    (ms.GetBuffer (), headers |> Array.append [|{ key = HeaderKey.ContentEncoding; value = Some compressionMethod }|])

                let (bytes, headers) = 
                    match request.header.AcceptEncoding with
                    | ContentEncoding.Deflate when compress -> 
                        compressStream (fun stream -> new DeflateStream (stream, System.IO.Compression.CompressionMode.Compress, true) :> Stream ) "deflate"
                    | ContentEncoding.GZip when compress -> 
                        compressStream (fun stream -> new GZipStream (stream, System.IO.Compression.CompressionMode.Compress, true) :> Stream ) "gzip"
                    | _ -> (bytes, headers)

                let headers = headers |> Array.append [|{ key = HeaderKey.ContentLength; value = Some (bytes.Length :> obj) }|]

                // TODO: ifModifiedSince
                // TODO: Expires

                // TODO: AsyncSendStream
                // var bytes = new byte[8192];
                // while (true)
                // {
                //     var read = await stream.ReadAsync(bytes, 0, bytes.Length);
                //     if (read == 0)
                //         return;
                //     await WriteAsync(bytes, 0, read);
                // }

                let bytes = 
                    match request.header.Method with
                    | Method.Head -> None
                    | _ -> Some bytes

                do! request.asyncSendBytes headers bytes
            }

        let asyncSendFile () =
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
                    do! asyncSendStream stream contentType lastModified
                with 
                | e -> request.categoryLogger.log LogLevel.Warning <| sprintf "Could not send file: %A" e
                ()
            } 
        async {
            // TODO: SendRange
            do! asyncSendFile ()
        }
