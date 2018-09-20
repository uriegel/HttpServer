namespace WebServer
open ActivePatterns

module Files =
    let serve request = 
        let path = (request.header HeaderKey.Path) :?> string
        let mutable query = None
        let path = 
            match path with
            | SplitChar '?' (path, part) -> 
                query <- Some part
                path
            | _ -> "path"
        printfn "%s" path
        ()