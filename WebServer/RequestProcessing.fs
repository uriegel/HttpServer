namespace WebServer
open FileSystem
open Microsoft.Extensions.Logging

module RequestProcessing =
    let configuration = Configuration.current.Force ()

    let asyncProcess socketSession request =

        let (|IsTlsRedirect|_|) request = 
            match socketSession.isSecure with
            | true -> None
            | false when configuration.IsTlsEnabled -> Some true
            | false -> None

        async {
            request.categoryLogger.log LogLevel.Trace (sprintf "Request: %A %s %s %s%s" socketSession.remoteEndPoint 
                (string request.header.Method) request.header.Path 
                (Header.getHttpVersionAsString request.header.HttpVersion)
                (if socketSession.isSecure then "" else " not secure"))
            
            match request with
            // TODO: TLS-Redirect als Option, aber ACME für Certbot priorisieren
            | IsTlsRedirect value -> 
                do! FixedResponses.asyncSendMovedPermanently socketSession request 
                        ("https://" + configuration.DomainName + 
                        (if configuration.TlsPort = 443 then "" else sprintf ":%d" configuration.TlsPort) + 
                        request.header.Path)
            | IsFileSystem value -> 
                match value with
                | File value -> do! serveFileSystem socketSession request value
                | Redirection value -> do! FixedResponses.asyncSendMovedPermanently socketSession request value
            | _ -> do! FixedResponses.asyncSendNotFound socketSession request
        }
