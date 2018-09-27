namespace WebServer
open ActivePatterns
open System

module Header11 = 
    let createHeaderAccess (headerString: string) =
        let headerParts = headerString.Split ([|"\r\n"|], StringSplitOptions.RemoveEmptyEntries)
        let method = 
            match headerParts with
                | _ when headerParts.[0].StartsWith("GET") -> Method.Get
                | _ when headerParts.[0].StartsWith("POST") -> Method.Post
                | _ when headerParts.[0].StartsWith("PUT") -> Method.Put
                | _ when headerParts.[0].StartsWith("DELETE") -> Method.Delete
                | _ when headerParts.[0].StartsWith("HEAD") -> Method.Head
                | _ when headerParts.[0].StartsWith("OPTIONS") -> Method.Options
                | _ when headerParts.[0].StartsWith("CONNECT") -> Method.Connect
                | _ when headerParts.[0].StartsWith("TRACE") -> Method.Trace
                | _ when headerParts.[0].StartsWith("PATCH") -> Method.Patch
                | _ -> failwith "Unknown HTTP Method"

        let startIndex = headerParts.[0].IndexOf (' ') + 1
        let path = headerParts.[0].Substring (startIndex, headerParts.[0].IndexOf(" HTTP") - startIndex)
        
        let startIndex = headerParts.[0].IndexOf (' ', startIndex) + 1
        let httpVersion = 
            match headerParts.[0].Substring startIndex with
            | InvariantEqual "HTTP/1.0" -> HttpVersion.Http1
            | InvariantEqual "HTTP/1.1" -> HttpVersion.Http11
            | _ -> failwith "Unknown HTTP protocol"

        let getHeaderValue headerKey = 
            let searchKey = 
                match headerKey with
                | HeaderKey.AcceptEncoding -> "accept-encoding"
                | _ -> ""
            headerParts
            |> Seq.tryFind (fun n -> (n.ToLower ()).StartsWith searchKey)

        {
            Method = method
            Path = path 
            HttpVersion = httpVersion
            AcceptEncoding = 
                match getHeaderValue HeaderKey.AcceptEncoding with
                | Some value when value.Contains("deflate") -> ContentEncoding.Deflate
                | Some value when value.Contains("gzip") -> ContentEncoding.GZip
                | _ -> ContentEncoding.None
        }

            


