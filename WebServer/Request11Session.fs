namespace WebServer

open System.IO
open System.Threading

module Request11Session =
    let mutable private idSeed = 0

    let private initialize socketSessionId =
        let id = Interlocked.Increment &idSeed
        sprintf "%d-%d" socketSessionId id

    let asyncStart socketSessionId (networkStream: Stream) =
        async {
            let id = initialize socketSessionId
            let! magicBytes = networkStream.AsyncRead 0
            ()
        }

