namespace WebServer
open System

module ResponseHeader =
    let configuration = Configuration.current.Force ()
    let prepare requestHeaders responseheaders =
        let headers = [
            { key = HeaderKey.Date; value = Some (DateTime.Now.ToUniversalTime () :> obj) }
            { key = HeaderKey.Server; value = Some ("URiegel" :> obj) }
        ] 
        let headers = 
            match configuration.xFrameOptions with 
            | XFrameOptions.NotSet -> headers
            | _ -> { key = HeaderKey.XFrameOptions; value = Some (configuration.xFrameOptions.ToString () :> obj) } :: headers
        
        // if (server.Configuration.AllowOrigins != null)
        // {
        //     var origin = requestHeaders["origin"];
        //     if (!string.IsNullOrEmpty(origin))
        //     {
        //         var host = requestHeaders["host"];
        //         if (string.Compare(origin, host, true) != 0)
        //         {
        //             var originToAllow = server.Configuration.AllowOrigins.FirstOrDefault(n => string.Compare(n, origin, true) == 0);
        //             if (originToAllow != null)
        //                 headers["Access-Control-Allow-Origin"] = originToAllow;
        //         }
        //     }
        // }

        

        // if (request.header HeaderKey.Path) :?> Method = Method.Options && headers.ContainsKey("Access-Control-Allow-Origin"))
        // // {
        // //     var request = requestHeaders["Access-Control-Request-Headers"];
        // //     if (request != null)
        // //         headers["Access-Control-Allow-Headers"] = request;
        // //     request = requestHeaders["Access-Control-Request-Method"];
        // //     if (request != null)
        // //         headers["Access-Control-Allow-Method"] = request;
        // // }
        responseheaders
        |> List.append headers 



