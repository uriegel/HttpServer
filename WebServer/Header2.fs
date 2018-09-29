namespace WebServer
open System
open HPack

module Header2 = 
    let createHeaderAccess headerFields = 

        let getAcceptEncoding (encodingString: string) = 
            if encodingString.Contains("deflate", StringComparison.InvariantCultureIgnoreCase) then
                ContentEncoding.Deflate
            elif encodingString.Contains("gzip", StringComparison.InvariantCultureIgnoreCase) then
                ContentEncoding.GZip
            else
                ContentEncoding.None

        // TODO: homePath: ""
        
        let getKeyValue headerField = 
            match headerField with  
            | FieldIndex fieldIndex -> 
                match fieldIndex with 
                | StaticIndex staticIndex -> 
                    match staticIndex with
                    | StaticTableIndex.MethodGET -> Some (HeaderKey.Method, Method.Get :> obj)
                    | StaticTableIndex.MethodPOST -> Some (HeaderKey.Method, Method.Post :> obj)
                    | StaticTableIndex.PathHome -> Some (HeaderKey.Path, "" :> obj)
                    | StaticTableIndex.PathIndexHtml -> Some (HeaderKey.Path, "/index.html" :> obj)
                    | _ -> None
                | _ -> None
            | Field field ->
                match field.Key with 
                | StaticIndex staticIndex ->
                    match staticIndex with
                    | StaticTableIndex.AcceptEncodingGzipDeflate -> Some (HeaderKey.AcceptEncoding, (getAcceptEncoding field.Value) :> obj)
                    | _ -> None
                | Key keyValue when keyValue = ":path" -> Some  (HeaderKey.Path, field.Value :> obj)
                | _ -> None

        let rec getAllKeyValue headerFieldList =
            match headerFieldList with
            | head :: tail -> 
                match tail with 
                | [] -> [getKeyValue head]
                | _ -> getKeyValue head :: getAllKeyValue tail
            | _ -> failwith "header list empty"

        let headers = 
            getAllKeyValue headerFields
            |> List.filter (fun n -> n.IsSome)
            |> List.map (fun n -> n.Value)
            |> Map.ofList

        {
            Method = 
                match headers.TryFind HeaderKey.Method with
                | Some value ->  value :?> Method
                | None -> failwith "no method"
            Path = 
                match headers.TryFind HeaderKey.Path with
                | Some value ->  string value 
                | None -> failwith "no path"
            HttpVersion = HttpVersion.Http2
            AcceptEncoding = 
                match headers.TryFind HeaderKey.AcceptEncoding with
                | Some value ->  value :?> ContentEncoding
                | None -> ContentEncoding.None
            IfModifiedSince = None
        }