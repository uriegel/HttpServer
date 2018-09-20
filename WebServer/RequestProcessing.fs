namespace WebServer
open Microsoft.Extensions.Logging

module RequestProcessing =
    let private httpVersionToString httpVersion =
        match httpVersion with
        | HttpVersion.Http1 -> "HTTP/1.0"
        | HttpVersion.Http11 -> "HTTP/1.1"
        | HttpVersion.Http2 -> "HTTP/2"
        | _ -> "HTTP??"

    let asyncProcess socketSession request =
        request.categoryLogger.log LogLevel.Trace (sprintf "Request: %A %s %s %s%s" socketSession.remoteEndPoint 
            (string (request.header HeaderKey.Method)) (string (request.header HeaderKey.Path)) 
            (httpVersionToString (request.header HeaderKey.HttpVersion :?> HttpVersion ))
            (if socketSession.isSecure then "" else " not secure"))
        
        match request.header HeaderKey.Path with
        | _ -> Files.serve request