namespace WebServer
open System
open System.Collections.Generic

module ResponseHeader =
    let prepare requestHeaders (responseheaders: ResponseHeaderValue[]) =
        let headerList = new List<ResponseHeaderValue>()
        headerList.Add { key = HeaderKey.Date; value = Some (DateTime.Now.ToUniversalTime () :> obj) }
        headerList.Add { key = HeaderKey.Server; value = Some (Constants.server :> obj) }
        if Configuration.Current.XFrameOptions <> XFrameOptions.NotSet then
            headerList.Add { key = HeaderKey.XFrameOptions; value = Some (Configuration.Current.XFrameOptions.ToString () :> obj) }
        // Not with LetsEncrypt
        // if (server.Configuration.HstsDurationInSeconds > 0)
        //     headers["Strict-Transport-Security"] = $"max-age={server.Configuration.HstsDurationInSeconds}";
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
        headerList.ToArray ()
        |> Array.append responseheaders



