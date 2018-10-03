namespace WebServer

module FixedResponses =
    let asyncSendNotModifed socketSession request =
        request.categoryLogger.lowTrace <| fun () -> "304 Not Modified"
        (
            request,
            [ { key = HeaderKey.Status304; value = None }],
            None
        )
        |> Response.asyncSendBytes

    let asyncSendMovedPermanently socketSession request location =
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

    let asyncSendNotFound socketSession request =
        request.categoryLogger.lowTrace <| fun () -> "404 Not Found"
        (
            request, 
            [{ key = HeaderKey.Status404; value = None }], 
            (Some <| Html.get "<h1>File not found</h1><p>The requested resource could not be found.</p>")
        )
        |> Response.insertHtml
        |> Response.asyncSendBytes
