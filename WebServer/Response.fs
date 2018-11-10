namespace WebServer
open System
open System.IO
open System.IO.Compression
open System.Runtime.Serialization.Json
open System.Text

module Response =
    let configuration = Configuration.current.Force ()

    let addContentLength (request, headers, (bytes: byte[] option)) = 
        match bytes with
        | Some bytes ->
            let contentLengthValue = { key = HeaderKey.ContentLength; value = Some (bytes.Length :> obj) }
            (request, contentLengthValue::headers, Some bytes)
        | None -> (request, headers, bytes)

    let tryCompress (contentType: string) (request, headers, bytes) = 
        match bytes with
        | Some bytes -> 
            let compress =  
                not configuration.noCompression 
                && (contentType.StartsWith ("application/javascript", StringComparison.CurrentCultureIgnoreCase)
                    || contentType.StartsWith ("text/", StringComparison.CurrentCultureIgnoreCase)
                    || contentType.StartsWith ("application/json", StringComparison.CurrentCultureIgnoreCase))
            
            let compressStream (streamCompressor: Stream->Stream) compressionMethod =
                use ms = new MemoryStream ()
                use compressedStream = streamCompressor ms
                compressedStream.Write (bytes, 0, bytes.Length) |> ignore
                compressedStream.Close ()
                ms.Capacity <- int ms.Length
                (
                    request, 
                    headers @ [{ key = HeaderKey.ContentEncoding; value = Some compressionMethod }], 
                    Some (ms.GetBuffer ())
                )

            match request.data.header.acceptEncoding with
            | ContentEncoding.Deflate when compress -> 
                compressStream (fun stream -> new DeflateStream (stream, System.IO.Compression.CompressionMode.Compress, true) :> Stream ) "deflate"
            | ContentEncoding.GZip when compress -> 
                compressStream (fun stream -> new GZipStream (stream, System.IO.Compression.CompressionMode.Compress, true) :> Stream ) "gzip"
            | _ -> (request, headers, Some bytes)
        | None -> (request, headers, None)

    let tryAddExpires (contentType: string) (request, headers, bytes) = 
        let headers = 
            if contentType.StartsWith ("application/", StringComparison.CurrentCultureIgnoreCase)
                || contentType.StartsWith ("text/css", StringComparison.CurrentCultureIgnoreCase)
                || contentType.StartsWith ("text/html", StringComparison.CurrentCultureIgnoreCase) then
                (headers |> List.append [{ key = HeaderKey.Expires; value = Some ((DateTime.Now.ToUniversalTime()).ToString "r" :> obj) }])
            else
                headers
        (request, headers, bytes)

        // TODO: AsyncSendStream
        // var bytes = new byte[8192];
        // while (true)
        // {
        //     var read = await stream.ReadAsync(bytes, 0, bytes.Length);
        //     if (read == 0)
        //         return;
        //     await WriteAsync(bytes, 0, read);
        // }

    let getJsonBytes data =
        let jason = DataContractJsonSerializer (data.GetType ())
        use memStm = new MemoryStream ()
        jason.WriteObject (memStm, data)
        memStm.Capacity <- int memStm.Length
        memStm.GetBuffer () 

    let insertHtml (request, responseHeaders, (html: string option)) =
        match html with
        | Some value -> 
            let responseHeaders = 
                responseHeaders @ [  
                    { key = HeaderKey.ContentLength; value = Some (value.Length :> obj) }  
                    { key = HeaderKey.ContentType; value = Some ("text/html; charset=UTF-8" :> obj) }  
                ]
            (request, responseHeaders, Some (Encoding.UTF8.GetBytes value))
        | None -> (request, responseHeaders, None)

    let asyncSendBytes (request, responseHeaders, bytes) =
        request.asyncSendBytes responseHeaders bytes

    let asyncSendJsonBytes request jsonBytes =
        let contentType = "application/json; charset=UTF-8"
        let headers = 
            [ 
                { key = HeaderKey.StatusOK; value = None }   
                { key = HeaderKey.ContentType; value = Some (contentType :> obj) }  
                { key = HeaderKey.CacheControl; value = Some ("no-cache,no-store" :> obj) }  
            ]
        
        (request, headers, jsonBytes)
        |> tryCompress contentType 
        |> addContentLength
        |> asyncSendBytes

    let asyncSendFileBytes request contentType (lastModified: DateTime) bytes =
        let headers = 
            [ 
                { key = HeaderKey.StatusOK; value = None }   
                { key = HeaderKey.ContentType; value = Some (contentType :> obj) }  
                { key = HeaderKey.LastModified; value = Some (lastModified :> obj) }  
            ]
        (request, headers, bytes)
        |> tryCompress contentType 
        |> addContentLength
        |> asyncSendBytes

    let asyncSendJson request data =
        let jsonBytes = Some (getJsonBytes data)
        asyncSendJsonBytes request jsonBytes

    let asyncSendJsonString request (json: string) =
        let jsonBytes = Some (Encoding.UTF8.GetBytes json)
        asyncSendJsonBytes request jsonBytes

    let createSseProcessor request = 
        let enqueuer = MailboxProcessor.Start(fun queue -> 
            // the message processing function
            let rec messageLoop() = async {
                let! ssePayload = queue.Receive()
                let bytes = Encoding.UTF8.GetBytes (s = ssePayload)
                do! request.asyncSendRaw bytes

                // loop to top
                return! messageLoop()  
            }

            // start the loop   
            messageLoop() 
        )

        let result event payload = 
            let ssePayload = sprintf "event: %s\r\ndata: %s\r\n\r\n" event payload
            enqueuer.Post ssePayload
        result
           
