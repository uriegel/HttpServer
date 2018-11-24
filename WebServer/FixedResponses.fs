namespace WebServer

module FixedResponses =
    let asyncSendNotModifed request =
        request.categoryLogger.lowTrace <| fun () -> "304 Not Modified"
        (
            request,
            [ { key = HeaderKey.Status304; value = None }],
            None
        )
        |> Response.asyncSendBytes

    let asyncSendMovedPermanently request location =
        request.categoryLogger.lowTrace <| fun () -> "301 Moved Permanently"
        (
            request,
            [ 
                { key = HeaderKey.Status301; value = None }  
                { key = HeaderKey.Location; value = Some location }
            ],
            (Some <| Html.get "<h1>Moved permanently</h1><p>The specified resource moved permanently.</p>")
        )
        |> Response.insertHtml
        |> Response.asyncSendBytes

    let asyncSendNotFound request =
        request.categoryLogger.lowTrace <| fun () -> "404 Not Found"
        (
            request, 
            [{ key = HeaderKey.Status404; value = None }], 
            (Some <| Html.get "<h1>File not found</h1><p>The requested resource could not be found.</p>")
        )
        |> Response.insertHtml
        |> Response.asyncSendBytes

    let asyncSendSseAccept request =
        request.categoryLogger.lowTrace <| fun () -> "Accepting SSE request"
        (
            request,
            [ 
                { key = HeaderKey.StatusOK; value = None }
                { key = HeaderKey.ContentType; value = Some ("text/event-stream" :> obj) }
            ],
            None
        )
        |> Response.asyncSendBytes

    let asyncSendServerError request =
        request.categoryLogger.lowTrace <| fun () -> "500 Server Error"
        (
            request,
            [ { key = HeaderKey.Status500; value = None } ],
            (Some <| Html.get "<h1>Error</h1><p>General Server Error occurred.</p>")
        )
        |> Response.insertHtml
        |> Response.asyncSendBytes
