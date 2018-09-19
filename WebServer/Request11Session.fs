﻿namespace WebServer

open System.IO
open System.Threading
open Microsoft.Extensions.Logging

module Request11Session =
    let mutable private idSeed = 0

    let private initialize socketSessionId =
        let id = Interlocked.Increment &idSeed
        sprintf "%d-%d" socketSessionId id

    let headerBytes = Array.zeroCreate 20000

    let asyncStart socketSessionId (networkStream: Stream) =
        let rec readHeader alreadyRead =
            async {
                let! read = networkStream.AsyncRead (headerBytes, alreadyRead, headerBytes.Length - alreadyRead)
                let alreadyRead = alreadyRead + read
                let header = System.Text.Encoding.UTF8.GetString (headerBytes, 0, alreadyRead)
                if header.Contains("\r\n\r\n") then
                    return (header, alreadyRead)
                else
                    return! readHeader alreadyRead
            }

        async {
            let id = initialize socketSessionId
            let! (headerString, alreadyRead) = readHeader 0
            let headers = Header11.createHeaderAccess headerString
            //Request.asyncProcess
            
            // TODO: Nimm ein Request-Object und übergib headers und payloadAccess
            // TODO: TLS-Redirect als Option, aber ACME für Certbot priorisieren
            let path = headers HeaderKey.Path
            let method = headers HeaderKey.Method
            ()
        }

