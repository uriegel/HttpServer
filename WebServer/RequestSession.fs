namespace WebServer

open System.IO
open System

module RequestSession =
    let asyncStart socketSessionId (networkStream: Stream) =
        let MAGIC = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"
        
        let mutable headerTableSize = 0
        let mutable windowUpdate = 0

        let log = Logger.log (string socketSessionId)
        let lowTrace = Logger.lowTrace (string socketSessionId)

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

        let rec asyncReadNextFrame () = 
            async {
                lowTrace (fun () -> "Reading next frame")
                let! frame = asyncReadFrame ()

                match frame with 
                | :? Headers as headers -> 
                    let flags = headers.Flags
                    use headerStream = headers.Stream
                    lowTrace (fun () -> sprintf "%d - Headers, E: %A, flags: %A, weight: %d" headers.StreamId headers.E headers.Flags headers.Weight)
                    // TODO: Type Decoder in session, set properties like HEADER_TABLE_SIZE
                    //let headerFields = HPack.decode headerStream
                    //do! asyncProcessRequest headers.StreamId headerFields
                    ()
                | :? Settings as settings -> 
                    match settings.Values.TryFind(SettingsIdentifier.HEADER_TABLE_SIZE) with 
                    | Some hts -> headerTableSize <- hts
                    | None -> ()
                    lowTrace (fun () -> sprintf "Settings: HeaderTableSize: %d" headerTableSize)
                    let ack = Settings.createAck settings.StreamId
                    do! asyncSendFrame ack
                | :? WindowUpdate as windowUpdateFrame -> 
                    windowUpdate <- windowUpdate + windowUpdateFrame.SizeIncrement
                    lowTrace (fun () -> sprintf "Window update: %d" windowUpdate)
                | :? RstStream as rstStream -> lowTrace (fun () -> sprintf "Reset stream, Error: %d" rstStream.Error)
                | :? Ping as ping -> 
                    lowTrace (fun () -> "ping received")
                    do! asyncSendFrame <| ping.createAck ()
                | _ -> failwith (sprintf "type not supported: %A" frame.Type)
                do! asyncReadNextFrame ()
            }

        async {
            let! magicBytes = networkStream.AsyncRead MAGIC.Length
            if magicBytes.[0] <> 80uy || magicBytes.[MAGIC.Length-1] <> 10uy then failwith "http2 magic prefix missing"
            do! asyncReadNextFrame () 
        }
