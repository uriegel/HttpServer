namespace WebServer
open FileSystem
open Microsoft.Extensions.Logging

module RequestProcessing =
    let asyncProcess socketSession request =
        async {
            request.categoryLogger.log LogLevel.Trace (sprintf "Request: %A %s %s %s%s" socketSession.remoteEndPoint 
                (string <| request.header HeaderKey.Method) (request.header HeaderKey.Path :?> string) 
                (Header.getHttpVersionAsString (request.header HeaderKey.HttpVersion :?> HttpVersion ))
                (if socketSession.isSecure then "" else " not secure"))
            
            match request with
            | IsFileSystem value -> serveFileSystem value
            // TODO: Redirection
            // TODO: Serve file
            | _ -> do! FixedResponses.asyncSendNotFound socketSession request
        }
