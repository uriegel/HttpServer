namespace WebServer
open FileSystem
open Microsoft.Extensions.Logging

module RequestProcessing =
    let configuration = Configuration.current.Force ()

    let asyncProcess socketSession request =

        let (|IsTlsRedirect|_|) request = 
            match socketSession.isSecure with
            | true -> None
            | false when configuration.isTlsEnabled -> Some true
            | false -> None

        let (|CheckExtension|_|) request = 
            match configuration.checkRequest request.header with
            | true -> Some true
            | false -> None

        let (|CheckSse|_|) request = 
            match configuration.sseCallback  with
            | Some value -> 
                match request.header.getValue HeaderKey.Accept with
                | Some acceptValue -> 
                    match acceptValue with
                    | "text/event-stream" -> Some true 
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
            | IsTlsRedirect value -> 
                do! FixedResponses.asyncSendMovedPermanently socketSession request 
                        ("https://" + configuration.domainName + 
                        (if configuration.tlsPort = 443 then "" else sprintf ":%d" configuration.tlsPort) + 
                        request.header.path)
            | CheckExtension _ -> do! configuration.request request
            | IsFileSystem value -> 
                match value with
                | File value -> do! serveFileSystem socketSession request value
                | Redirection value -> do! FixedResponses.asyncSendMovedPermanently socketSession request value
            | CheckSse _ -> do! FixedResponses.asyncSendSseAccept socketSession request
            | _ -> do! FixedResponses.asyncSendNotFound socketSession request
        }
