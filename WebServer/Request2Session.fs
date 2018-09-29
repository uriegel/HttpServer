namespace WebServer

open System
open System.IO
open Header2

module RequestSession =
    let MAGIC = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"

    /// Maximium header size allowed
    let HTTP_MAX_HEADER_SIZE = 80 * 1024

    let asyncStart socketSession (networkStream: Stream) stopwatch =
        
        let mutable headerTableSize = 0
        let mutable windowUpdate = 0

        let logger = {
            log = Logger.log <| string socketSession.id
            lowTrace = Logger.lowTrace <| string socketSession.id
        }

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

        let asyncSendFrame (frame: Frame) = 
            async {
                let bytes = frame.serialize ()
                do! networkStream.AsyncWrite (bytes, 0, bytes.Length)
            }

        let asyncSendBytes headerAccess streamId (responseHeaders: ResponseHeaderValue[]) (payload: byte[] option) = 
            async {
                let responseHeaders = ResponseHeader.prepare headerAccess responseHeaders
                let headerFields = 
                    responseHeaders
                    |> Array.map (fun n -> 
                        match n.key with
                        | HeaderKey.StatusOK -> HPack.FieldIndex (HPack.StaticIndex StaticTableIndex.Status200)
                        | HeaderKey.Status404 -> HPack.FieldIndex (HPack.StaticIndex StaticTableIndex.Status404)
                        //| HeaderKey.Status301 -> HPack.FieldIndex (HPack.StaticIndex StaticTableIndex.Status301) // TODO: Gibts nicht
                        | HeaderKey.Status304 -> HPack.FieldIndex (HPack.StaticIndex StaticTableIndex.Status304)
                        | HeaderKey.ContentType -> HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.ContentType); Value = string n.value } 
                        | HeaderKey.ContentLength -> HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.ContentLength); Value = string n.value } 
                        | HeaderKey.ContentEncoding -> HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.ContentEncoding); Value = string n.value } 
                        | HeaderKey.Expires -> HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.Expires); Value = string n.value } 
                        | HeaderKey.Date -> 
                            HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.Date); Value = (n.value.Value :?> DateTime).ToString "R" } 
                        | HeaderKey.Server -> HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.Server); Value = string n.value } 
                        | HeaderKey.LastModified -> 
                            HPack.Field { Key = (HPack.StaticIndex StaticTableIndex.LastModified); Value = (n.value.Value :?> DateTime).ToString "R" } 
                        | _ -> failwith "Not supported"
                    )
                    |> Array.toList 

                let encodedHeaderFields = HPack.encode headerFields
                let receiveHeaders = Headers.create streamId encodedHeaderFields true

                let bytes = receiveHeaders.serialize ()
                do! networkStream.AsyncWrite (bytes, 0, bytes.Length)

                match payload with
                | Some payload ->
                    let bytes = Array.zeroCreate (payload.Length + 9)
                    let lengthBytes = BitConverter.GetBytes payload.Length
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
                    System.Array.Copy(payload, 0, bytes, 9, payload.Length)
                    do! networkStream.AsyncWrite (bytes, 0, bytes.Length)
                | None -> ()
            }

        let rec asyncReadNextFrame () = 
            async {
                logger.lowTrace (fun () -> "Reading next frame")
                let! frame = asyncReadFrame ()

                match frame with 
                | :? Headers as headers -> 
                    let flags = headers.Flags
                    use headerStream = headers.Stream
                    logger.lowTrace (fun () -> sprintf "%d - Headers, E: %A, flags: %A, weight: %d" headers.StreamId headers.E headers.Flags headers.Weight)
                    // TODO: Type Decoder in session, set properties like HEADER_TABLE_SIZE
                    let headerFields = HPack.decode headerStream
                    let headerAccess = createHeaderAccess headerFields

                    do! RequestProcessing.asyncProcess socketSession {
                        categoryLogger = logger
                        header = headerAccess
                        asyncSendBytes = asyncSendBytes headerAccess headers.StreamId
                    }                    
                | :? Settings as settings -> 
                    match settings.Values.TryFind(SettingsIdentifier.HEADER_TABLE_SIZE) with 
                    | Some hts -> headerTableSize <- hts
                    | None -> ()
                    logger.lowTrace (fun () -> sprintf "Settings: HeaderTableSize: %d" headerTableSize)
                    let ack = Settings.createAck settings.StreamId
                    do! asyncSendFrame ack
                | :? WindowUpdate as windowUpdateFrame -> 
                    windowUpdate <- windowUpdate + windowUpdateFrame.SizeIncrement
                    logger.lowTrace (fun () -> sprintf "Window update: %d" windowUpdate)
                | :? RstStream as rstStream -> logger.lowTrace (fun () -> sprintf "Reset stream, Error: %d" rstStream.Error)
                | :? Ping as ping -> 
                    logger.lowTrace (fun () -> "ping received")
                    do! asyncSendFrame <| ping.createAck ()
                | _ -> failwith (sprintf "type not supported: %A" frame.Type)
                do! asyncReadNextFrame ()
            }

        async {
            let! magicBytes = networkStream.AsyncRead MAGIC.Length
            if magicBytes.[0] <> 80uy || magicBytes.[MAGIC.Length-1] <> 10uy then failwith "http2 magic prefix missing"
            do! asyncReadNextFrame () 
            return true
        }
