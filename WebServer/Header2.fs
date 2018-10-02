namespace WebServer
open System
open HPack

module Header2 = 
    let createHeaderAccess (headers: Map<HPack.Index,string option>) = 
        let (|FindMethod|_|) (method: Index) (arg: Map<HPack.Index,string option>) = 
            match arg.TryFind method with
            | Some value -> 
                match method with
                | HPack.StaticIndex StaticTableIndex.MethodGET -> Some Method.Get
                | HPack.StaticIndex StaticTableIndex.MethodPOST -> Some Method.Post 
                | _ -> None
            | None -> None

        let method =
            match headers with
            | FindMethod (HPack.StaticIndex StaticTableIndex.MethodGET) value -> value
            | FindMethod (HPack.StaticIndex StaticTableIndex.MethodPOST) value -> value
            | _ -> failwith "No method"

        let path = 
            match headers.TryFind(HPack.StaticIndex StaticTableIndex.PathHome) with
            | Some value -> "/index.html"
            | None -> 
                match headers.TryFind(HPack.StaticIndex StaticTableIndex.PathIndexHtml) with
                | Some value -> "/index.html"
                | None -> 
                    match headers.TryFind(HPack.Key ":path") with
                    | Some value -> 
                        match value with 
                        | Some value -> value
                        | None -> failwith "unknown path"
                    | None -> failwith "unknown path"

        let acceptEncoding = 
            match headers.TryFind(HPack.StaticIndex StaticTableIndex.AcceptEncodingGzipDeflate) with
            | Some value ->     
                if value.Value.Contains("deflate", StringComparison.InvariantCultureIgnoreCase) then
                    ContentEncoding.Deflate
                elif value.Value.Contains("gzip", StringComparison.InvariantCultureIgnoreCase) then
                    ContentEncoding.GZip
                else
                    ContentEncoding.None
            | None -> ContentEncoding.None

        {
            method = method
            path = path
            httpVersion = HttpVersion.Http2
            acceptEncoding = acceptEncoding
            ifModifiedSince = None
        }