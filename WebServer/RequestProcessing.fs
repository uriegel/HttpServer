namespace WebServer
open FileSystem
open Microsoft.Extensions.Logging

module RequestProcessing =
    let asyncProcess socketSession request =

        let (|IsTlsRedirect|_|) request = 
            match socketSession.isSecure with
            | true -> None
            | false when Configuration.Current.IsTlsEnabled -> Some true
            | false -> None

        async {
            request.categoryLogger.log LogLevel.Trace (sprintf "Request: %A %s %s %s%s" socketSession.remoteEndPoint 
                (string <| request.header HeaderKey.Method) (request.header HeaderKey.Path :?> string) 
                (Header.getHttpVersionAsString (request.header HeaderKey.HttpVersion :?> HttpVersion ))
                (if socketSession.isSecure then "" else " not secure"))
            
            match request with
            // TODO: TLS-Redirect als Option, aber ACME für Certbot priorisieren
            | IsTlsRedirect value -> 
                do! FixedResponses.asyncSendMovedPermanently socketSession request 
                        ("https://" + Configuration.Current.DomainName + 
                        (if Configuration.Current.TlsPort = 443 then "" else sprintf ":%d" Configuration.Current.TlsPort) + 
                        (request.header HeaderKey.Path :?> string))
            | IsFileSystem value -> serveFileSystem value
            // TODO: Redirection
            // TODO: Serve file
            | _ -> do! FixedResponses.asyncSendNotFound socketSession request

            // TODO: Stopwatch-Ausgabe, wie lange request dauerte
        }
