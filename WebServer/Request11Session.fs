namespace WebServer

open System.IO
open System.Threading
open System
open System.Text

module Request11Session =
    let mutable private idSeed = 0

    let private initialize socketSessionId =
        let id = Interlocked.Increment &idSeed
        sprintf "%d-%d" socketSessionId id

    let headerBytes = Array.zeroCreate 20000

    let asyncStart socketSession (networkStream: Stream) =

        let (|IsStatus|_|) value responseHeaders = 
            let found = 
                responseHeaders 
                |> Array.tryFind  (fun n -> n.key = value)

            if found.IsSome then 
                Some "404 Not found"
            else
                None 

        let rec readHeader alreadyRead =
            async {
                let! read = networkStream.AsyncRead (headerBytes, alreadyRead, headerBytes.Length - alreadyRead)
                let alreadyRead = alreadyRead + read
                let header = System.Text.Encoding.UTF8.GetString (headerBytes, 0, alreadyRead)
                if header.Contains "\r\n\r\n" then
                    return (header, alreadyRead)
                else
                    return! readHeader alreadyRead
            }

        async {
            let id = initialize socketSession.id

            let logger = {
                log = Logger.log id
                lowTrace = Logger.lowTrace id
            }

            let! (headerString, alreadyRead) = readHeader 0
            let headers = Header11.createHeaderAccess headerString

            let asyncSendBytes responseHeaders bytes = 
                async {
                    let responseHeaders = ResponseHeader.prepare headers responseHeaders

                    let headersToSerialize = responseHeaders |> Array.filter (fun n -> n.key <> HeaderKey.Status404)

                    // TODO:
                    // if (!headers.ContainsKey("Content-Length"))
                    //     headers["Connection"] = "close";

                    let createHeaderStringValue responseHeaderValue = 
                        let key = 
                            match responseHeaderValue.key with
                            | HeaderKey.ContentLength -> "Content-Length"
                            | HeaderKey.ContentType -> "Content-Type"
                            | _ -> responseHeaderValue.key.ToString ()
                        let value = 
                            match responseHeaderValue.value with
                            | Some value ->
                                match value with    
                                | :? string as value -> value
                                | :? int as value -> value.ToString ()
                                | :? DateTime as value -> value.ToString "R"
                                | _ -> failwith "Wrong value type"
                            | None -> failwith "No value"
                        key + ": " + value

                    let createStatus () = 
                        match responseHeaders with
                            | IsStatus HeaderKey.Status404 value -> value
                            | _ -> failwith "No status"

                    let headerStrings = headersToSerialize |> Array.map createHeaderStringValue
                    let headerString = Header.getHttpVersionAsString (headers HeaderKey.HttpVersion :?> HttpVersion) + " " + (createStatus ()) + "\r\n" 
                                        + System.String.Join ("\r\n", headerStrings) + "\r\n\r\n" 
                    let headerBytes = Encoding.UTF8.GetBytes headerString
                    do! networkStream.AsyncWrite (headerBytes, 0, headerBytes.Length)
                    do! networkStream.AsyncWrite (bytes, 0, bytes.Length)
                }

            do! RequestProcessing.asyncProcess socketSession {
                categoryLogger = logger
                header = headers
                asyncSendBytes = asyncSendBytes
            }
            
            // TODO: TLS-Redirect als Option, aber ACME für Certbot priorisieren
        }




