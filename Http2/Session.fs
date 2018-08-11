namespace Http2
open System
open System.IO
open System.Threading.Tasks

type Session(networkStream: Stream) = 
    
    let MAGIC = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"

    /// Maximium header size allowed
    let HTTP_MAX_HEADER_SIZE = 80 * 1024

    let mutable headerTableSize= 0

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
                | _ -> failwith "Type not supported"
        }

    let asyncSendFrameAsync (frame: Frame) = 
        async {
            let bytes = frame.serialize ()
            do! networkStream.AsyncWrite (bytes, 0, bytes.Length)
        }

    let rec asyncReadNextFrame () = 
        async {
            let! frame = asyncReadFrame ()
            match frame with 
            | :? Headers as headers -> 
                let flags = headers.Flags
                use headerStream = headers.Stream
                // TODO: Type Decoder in session, set properties like HEADER_TABLE_SIZE
                let headerFileds = HPack.Decode headerStream
                ()
            | :? Settings as settings -> 
                match settings.Values.TryFind(SettingsIdentifier.HEADER_TABLE_SIZE) with 
                | Some hts -> headerTableSize <- hts
                | None -> ()
                let ack = Settings.createAck settings.StreamId
                do! asyncSendFrameAsync ack
            | :? WindowUpdate as windowUpdate -> 
                let w = windowUpdate.SizeIncrement
                ()
            | _ -> failwith "type not supported"
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
