namespace Http2
open System
open System.IO
open System.Threading.Tasks
open System.Diagnostics
open System.Text

type Method = 
    GET = 0uy
    | POST = 1uy

type Session(networkStream: Stream) = 
    
    let MAGIC = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"

    /// Maximium header size allowed
    let HTTP_MAX_HEADER_SIZE = 80 * 1024

    let mutable headerTableSize= 0
    let mutable windowUpdate = 0

    let asyncReadFrame () = 
        async {
            let asyncRead length = 
                async {
                    let bytes = Array.zeroCreate length

                    let rec asyncRead alreadyRead = 
                        async {
                            let! read = networkStream.AsyncRead (bytes, alreadyRead, bytes.Length - alreadyRead)  
                            let total = read + alreadyRead
                            if total = length then ()
                            else do! asyncRead total
                        }
                    do! asyncRead 0
                    return bytes
                }
            let! header = asyncRead Frame.SIZE
            let length = BitConverter.ToInt32 ([| header.[2]; header.[1]; header.[0]; 0uy |], 0)
            let! payload = asyncRead length
            return 
                match LanguagePrimitives.EnumOfValue<byte, FrameType> header.[3] with 
                | FrameType.HEADERS -> Headers (header, payload) :> Frame
                | FrameType.SETTINGS -> Settings (header, payload) :> Frame
                | FrameType.WINDOW_UPDATE -> WindowUpdate (header, payload) :> Frame
                | FrameType.RST_STREAM -> RstStream (header, payload) :> Frame
                | FrameType.PING -> Ping (header, payload) :> Frame
                | _ -> failwith (sprintf "type not supported: %A" (LanguagePrimitives.EnumOfValue<byte, FrameType> header.[3]))
        }

    let asyncSendFrameAsync (frame: Frame) = 
        async {
            let bytes = frame.serialize ()
            do! networkStream.AsyncWrite (bytes, 0, bytes.Length)
        }

    let asyncSendError streamId (htmlHead: string) (htmlBody: string) errorCode (errorText: string) =
        async {
            let response = sprintf "<html><head>%s</head><body>%s</body></html>" htmlHead htmlBody
            let responseBytes = Encoding.UTF8.GetBytes response
            // TODO: Deflate or GZip
            let contentLength = responseBytes.Length
            let headerFields = [ 
                HPack.FieldIndex (HPack.StaticIndex StaticTableIndex.Status404)
                HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.ContentType); Value = "text/html; charset=UTF-8"} 
                HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.ContentLength); Value = string contentLength} 
                HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.Date); Value = (DateTime.Now.ToUniversalTime ()).ToString("R")} 
                HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.Server); Value = "Uriegel Web Server"} 
            ]   

        
            let encodedHeaderFields = HPack.encode headerFields
            let receiveHeaders = Headers.create streamId encodedHeaderFields true

            let bytes = receiveHeaders.serialize ()
            do! networkStream.AsyncWrite (bytes, 0, bytes.Length)

            let headerFields = HPack.decode receiveHeaders.Stream

            let bytes = Array.zeroCreate (contentLength + 9)
            let lengthBytes = BitConverter.GetBytes contentLength
            let streamIdBytes = BitConverter.GetBytes streamId
            bytes.[0] <- lengthBytes.[2]
            bytes.[1] <- lengthBytes.[1]
            bytes.[2] <- lengthBytes.[0]
            bytes.[3] <- byte FrameType.DATA
            bytes.[4] <- 1uy
            bytes.[5] <- streamIdBytes.[3] &&& ~~~1uy
            bytes.[6] <- streamIdBytes.[2]
            bytes.[7] <- streamIdBytes.[1]
            bytes.[8] <- streamIdBytes.[0]
            System.Array.Copy(responseBytes, 0, bytes, 9, responseBytes.Length)
            do! networkStream.AsyncWrite (bytes, 0, bytes.Length)
        }
    
    let asyncSendNotFound streamId =
        async {
            // Logger.Current.LowTrace(() => $"{Id} 404 Not Found");
            do! asyncSendError streamId @"<title>URIEGEL</title>
<Style> 
html {
    font-family: sans-serif;
}
h1 {
    font-weight: 100;
}
</Style>" 
                    "<h1>Datei nicht gefunden</h1><p>Die angegebene Resource konnte auf dem Server nicht gefunden werden.</p>" 404 ""
        }

    let asyncProcessRequest streamId (headerFields: HPack.HeaderField[]) = 
        async {
            let headers = 
                headerFields
                |> Seq.map (fun n ->
                    match n with
                    | HPack.FieldIndex fieldIndex -> (fieldIndex, None)
                    | HPack.Field field -> (field.Key, Some field.Value)
                )
                |> Map.ofSeq
            let method =
                match headers.TryFind(HPack.StaticIndex StaticTableIndex.MethodGET) with
                | Some value -> Method.GET
                | None -> 
                    match headers.TryFind(HPack.StaticIndex StaticTableIndex.MethodPOST) with
                    | Some value -> Method.POST
                    | None -> failwith "unknown method"

            let path = 
                match headers.TryFind(HPack.StaticIndex StaticTableIndex.PathHome) with
                | Some value -> "/index.html"
                | None -> 
                    match headers.TryFind(HPack.StaticIndex StaticTableIndex.PathIndexHtml) with
                    | Some value -> "/index.html"
                    | None -> 
                        match headers.TryFind(HPack.Key ":path") with
                        | Some value -> 
                            match value with 
                            | Some value -> value
                            | None -> failwith "unknown path"
                        | None -> failwith "unknown path"

            //let headerFields = [ 
            //    HPack.FieldIndex (HPack.StaticIndex StaticTableIndex.MethodGET)
            //    HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.Authority); Value = "cas-ew-caesar-3.ub2.cae.local:8080"} // TODO: very long string > 256 chars
            //    HPack.Field { Key = (HPack.Key "upgrade-insecure-requests"); Value = "1"} // TODO: very long string > 256 chars
            //    HPack.FieldIndex (HPack.StaticIndex StaticTableIndex.MethodPOST)
            //]

            let headerFields = [ 
                HPack.FieldIndex (HPack.StaticIndex StaticTableIndex.Status404)
                HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.ContentType); Value = "text/html; charset=UTF-8"} 
                HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.ContentLength); Value = "1258"} 
                HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.Date); Value = (DateTime.Now.ToUniversalTime ()).ToString("R")} 
                HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.Server); Value = "Uriegel Web Server"} 
            ]

            //let headerFields = [  ]
            let testDecoded = HPack.encode headerFields

            let stream = new MemoryStream (testDecoded)

            let test = HPack.decode stream
        
            do! asyncSendNotFound streamId 
        }
        
        // TODO: SendNotFound -> sendHeader, send Frame
        // TODO Then change 404 to 200
        // TODO next request will use indexes from the dynamic table

    let rec asyncReadNextFrame () = 
        async {
            let! frame = asyncReadFrame ()

            printfn "Fram: %A" frame.Type
            match frame with 
            | :? Headers as headers -> 
                let flags = headers.Flags
                use headerStream = headers.Stream
                // TODO: Type Decoder in session, set properties like HEADER_TABLE_SIZE
                let headerFields = HPack.decode headerStream
                do! asyncProcessRequest headers.StreamId headerFields
                ()
            | :? Settings as settings -> 
                match settings.Values.TryFind(SettingsIdentifier.HEADER_TABLE_SIZE) with 
                | Some hts -> headerTableSize <- hts
                | None -> ()
                let ack = Settings.createAck settings.StreamId
                do! asyncSendFrameAsync ack
            | :? WindowUpdate as windowUpdateFrame -> 
                windowUpdate <- windowUpdate + windowUpdateFrame.SizeIncrement
            | :? RstStream as rstStream -> 
                let error = rstStream.Error
                ()
            | :? Ping as ping -> 
                do! asyncSendFrameAsync <| ping.createAck ()
            | _ -> failwith (sprintf "type not supported: %A" frame.Type)
            do! asyncReadNextFrame ()
        } 


    member this.StartAsync () =
        let completion = TaskCompletionSource<bool> ()

        async {
            try 
                let! magicBytes = networkStream.AsyncRead MAGIC.Length
                if magicBytes.[0] <> 80uy || magicBytes.[MAGIC.Length-1] <> 10uy then failwith "http2 magic prefix missing"
                do! asyncReadNextFrame () 
            with error -> completion.SetException(error)
        }|> Async.StartImmediate

        completion.Task
