﻿namespace Http2
open System
open System.IO
open System.Threading.Tasks

type Session(networkStream: Stream) = 
    
    let MAGIC = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"

    let asyncReadFrame () = 
        async {
            let asyncRead length = 
                async {
                    let bytes = Array.zeroCreate length

                    let rec asyncRead read = 
                        async {
                            let! read = networkStream.AsyncRead (bytes, read, bytes.Length - read) 
                            match read with
                            | 0 -> ()
                            | _ -> do! asyncRead read
                        }
                    do! asyncRead 0
                    return bytes
                }
            let! header = asyncRead Frame.SIZE
            let length = BitConverter.ToInt32 ([| header.[2]; header.[1]; header.[0]; 0uy |], 0)
            let frameType = LanguagePrimitives.EnumOfValue<byte, FrameType> header.[3]
            ()
        }

    let rec asyncReadNextFrame () = 
        async {
            let! bytes = asyncReadFrame ()
            ()
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
