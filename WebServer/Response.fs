namespace WebServer
open System
open System.IO
open System.IO.Compression

module Response =
    let tryCompress request (contentType: string) bytes = 
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

        match request.header.AcceptEncoding with
        | ContentEncoding.Deflate when compress -> 
            compressStream (fun stream -> new DeflateStream (stream, System.IO.Compression.CompressionMode.Compress, true) :> Stream ) "deflate"
        | ContentEncoding.GZip when compress -> 
            compressStream (fun stream -> new GZipStream (stream, System.IO.Compression.CompressionMode.Compress, true) :> Stream ) "gzip"
        | _ -> (bytes, headers)

    let asyncSend request (contentType: string) bytes headers =
        async {
            let headers = 
                if contentType.StartsWith ("application/javascript", StringComparison.CurrentCultureIgnoreCase)
                    || contentType.StartsWith ("text/css", StringComparison.CurrentCultureIgnoreCase)
                    || contentType.StartsWith ("text/html", StringComparison.CurrentCultureIgnoreCase) then
                    (headers |> Array.append [|{ key = HeaderKey.Expires; value = Some ((DateTime.Now.ToUniversalTime()).ToString "r" :> obj) }|])
                else
                    headers

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

            do! request.asyncSendBytes request headers bytes
        }