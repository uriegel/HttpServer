namespace WebServer
open System.Text
open System.IO
open System.IO.Compression
open Microsoft.Extensions.Logging
open FileSystem

module RequestProcessing =
    let configuration = Configuration.current.Force ()

    let asyncProcess socketSession request =

        let (|IsTlsRedirect|_|) _ = 
            match socketSession.isSecure with
            | true -> None
            | false when configuration.isTlsEnabled -> Some ()
            | false -> None

        let (|CheckExtension|_|) request = 
            match configuration.checkRequest request.header with
            | true -> Some ()
            | false -> None

        let (|CheckSse|_|) request = 
            match configuration.serverSentEvent with
            | Some value -> 
                match request.header.getValue HeaderKey.Accept with
                | Some acceptValue -> 
                    match acceptValue with
                    | "text/event-stream" -> Some value
                    | _ -> None
                | None -> None
            | None -> None

        async {
            request.categoryLogger.log LogLevel.Trace (sprintf "Request: %A %s %s %s%s" socketSession.remoteEndPoint 
                (string request.header.method) request.header.path 
                (Header.getHttpVersionAsString request.header.httpVersion)
                (if socketSession.isSecure then "" else " not secure"))
            
            match request with
            // TODO: TLS-Redirect als Option, aber ACME für Certbot priorisieren
            | IsTlsRedirect -> 
                do! FixedResponses.asyncSendMovedPermanently socketSession request 
                        ("https://" + configuration.domainName + 
                        (if configuration.tlsPort = 443 then "" else sprintf ":%d" configuration.tlsPort) + 
                        request.header.path)
            | CheckExtension -> do! configuration.request request
            | IsFileSystem value -> 
                match value with
                | File value -> do! serveFileSystem socketSession request value
                | Redirection value -> do! FixedResponses.asyncSendMovedPermanently socketSession request value
            | CheckSse value -> 
                value request |> ignore
                do! FixedResponses.asyncSendSseAccept socketSession request
            | _ -> do! FixedResponses.asyncSendNotFound socketSession request
        }
