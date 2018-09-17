namespace WebServer

open System.IO
open System.Threading
open Microsoft.Extensions.Logging

module Request11Session =
    let mutable private idSeed = 0

    let private initialize socketSessionId =
        let id = Interlocked.Increment &idSeed
        sprintf "%d-%d" socketSessionId id

    let asyncStart socketSessionId (networkStream: Stream) =
        async {
            let id = initialize socketSessionId
            let headerBytes = Array.zeroCreate 8192
            let! read = networkStream.AsyncRead headerBytes
            let hedder = System.Text.Encoding.UTF8.GetString (headerBytes)
            // TODO: wenn header nicht vollständig, erneut einlesen und string concatten, bis \r\n\r\n enthalten

            ()
        }

