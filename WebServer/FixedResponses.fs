namespace WebServer
open System.Text

module FixedResponses =
    let asyncSend socketSession request headers (html: string) = 
        async {
            let responseBytes = Encoding.UTF8.GetBytes html
            let headers = 
                [|  
                   { key = HeaderKey.ContentLength; value = Some (responseBytes.Length :> obj) }  
                   { key = HeaderKey.ContentType; value = Some ("text/html; charset=UTF-8" :> obj) }  
                |] 
                |> Array.append headers

            do! request.asyncSendBytes headers responseBytes
        }

    let asyncSendNotFound socketSession request =
        async {
            request.categoryLogger.lowTrace <| fun () -> "404 Not Found"
            let headers = 
                [|  
                   { key = HeaderKey.Status404; value = None }  
                |]
            do! asyncSend socketSession request headers <| Html.get "<h1>File not found</h1><p>The requested resource could not be found.</p>" 
        }

    // TODO: Connection Close
    let asyncSendMovedPermanently socketSession request location =
        async {
            request.categoryLogger.lowTrace <| fun () -> "301 Moved Permanently"
            let headers = 
                [|  
                   { key = HeaderKey.Status301; value = None }  
                   { key = HeaderKey.Location; value = Some location }  
                |]
            do! asyncSend socketSession request headers <| Html.get "<h1>Moved permanently</h1><p>The specified resource moved permanently.</p>" 
        }