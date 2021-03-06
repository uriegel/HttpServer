﻿namespace WebServer

open System.IO
open System.Threading
open System
open System.Text
open System.Diagnostics

module Request11Session =
    let mutable private idSeed = 0
    let configuration = Configuration.current.Force ()

    let private initialize socketSessionId =
        let id = Interlocked.Increment &idSeed
        sprintf "%d-%d" socketSessionId id

    let asyncStart socketSession (networkStream: Stream) (stopwatch: Stopwatch) =
        let headerBytes = Array.zeroCreate 20000

        let (|IsStatus|_|) value responseHeaders = 
            let found = 
                responseHeaders 
                |> List.tryFind  (fun n -> n.key = value)

            if found.IsSome then 
                Some true
            else
                None 

        let rec readHeader alreadyRead =
            async {
                let! read = networkStream.AsyncRead (headerBytes, alreadyRead, headerBytes.Length - alreadyRead)
                if read > 0 then
                    if not stopwatch.IsRunning then stopwatch.Start () |> ignore

                    let alreadyRead = alreadyRead + read
                    let header = System.Text.Encoding.UTF8.GetString (headerBytes, 0, alreadyRead)
                    if header.Contains "\r\n\r\n" then
                        return Some (header, alreadyRead)
                    else
                        return! readHeader alreadyRead
                else
                    return None
            }

        async {
            let id = initialize socketSession.id

            let logger = {
                log = Logger.log id
                lowTrace = Logger.lowTrace id
            }

            let! readResult = readHeader 0
            match readResult with
            | Some (headerString, alreadyRead) -> 
                let headers = Header11.createHeaderAccess headerString

                let asyncSendBytes responseHeaders bytes = 
                    async {
                        let responseHeaders = ResponseHeader.prepare headers responseHeaders

                        let headersToSerialize = responseHeaders |> List.filter (fun n -> n.key <> HeaderKey.StatusOK
                                                                                        && n.key <> HeaderKey.Status304
                                                                                        && n.key <> HeaderKey.Status404
                                                                                        && n.key <> HeaderKey.Status301
                                                                                        && n.key <> HeaderKey.Status500)
                        // TODO:
                        // if (!headers.ContainsKey("Content-Length"))
                        //     headers["Connection"] = "close";

                        let createHeaderStringValue responseHeaderValue = 
                            let key = 
                                match responseHeaderValue.key with
                                | HeaderKey.ContentLength -> "Content-Length"
                                | HeaderKey.ContentType -> "Content-Type"
                                | HeaderKey.ContentEncoding -> "Content-Encoding"
                                | HeaderKey.Expires -> "Expires"
                                | HeaderKey.LastModified -> "Last-Modified"
                                | _ -> responseHeaderValue.key.ToString ()
                            let value = 
                                match responseHeaderValue.value with
                                | Some value ->
                                    match value with    
                                    | :? string as value -> value
                                    | :? DateTime as value -> value.ToString "R"
                                    | _ -> value.ToString ()
                                | None -> failwith "No value"
                            key + ": " + value

                        let createStatus () = 
                            match responseHeaders with
                                | IsStatus HeaderKey.StatusOK _ -> "200 OK"
                                | IsStatus HeaderKey.Status404 _ -> "404 Not Found"
                                | IsStatus HeaderKey.Status304 _ -> "304 Not Modified"
                                | IsStatus HeaderKey.Status301 _ -> "301 Moved Permanently"
                                | IsStatus HeaderKey.Status500 _ -> "500 Server Error"
                                | _ -> failwith "No status"

                        let headerStrings = headersToSerialize |> List.map createHeaderStringValue
                        let headerString = Header.getHttpVersionAsString headers.httpVersion + " " + (createStatus ()) + "\r\n" 
                                            + System.String.Join ("\r\n", headerStrings) + "\r\n\r\n" 
                        let headerBytes = Encoding.UTF8.GetBytes headerString
                        do! networkStream.AsyncWrite (headerBytes, 0, headerBytes.Length)
                        match bytes with
                        | Some value -> do! networkStream.AsyncWrite (value, 0, value.Length)
                        | _ -> ()
                    }

                let asyncSendRaw bytes = 
                    networkStream.AsyncWrite (bytes, 0, bytes.Length)

                let request = {
                    data = {
                        socketSessionId = socketSession.id
                        header = headers
                    }
                    categoryLogger = logger
                    asyncSendBytes = asyncSendBytes
                    asyncSendRaw = asyncSendRaw
                }

                do! RequestProcessing.asyncProcess socketSession request
                let timeSpan = stopwatch.Elapsed
                logger.lowTrace (fun () -> sprintf "Request processed in %A" timeSpan)
                return true
            | None -> 
                logger.lowTrace <| fun () -> "Socket session closed"
                match configuration.sessionClosed with
                | Some socketClosed -> socketClosed socketSession.id
                | None -> ()
                return false
        }



