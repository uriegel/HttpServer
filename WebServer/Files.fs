namespace WebServer
open System
open Microsoft.Extensions.Logging
open ActivePatterns
open System.IO

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

        let  localUrl = Uri.UnescapeDataString path            

            //         var alias = Server.Configuration.Aliases.FirstOrDefault(n => url.StartsWith(n.Value));
            // if (alias != null)
            // {
            //     relativePath = localURL.Substring(alias.Value.Length).Replace('/', '\\');

            //     if (alias.IsRooted)
            //         rootDirectory = alias.Path;
            //     else
            //         rootDirectory = Path.Combine(rootDirectory, alias.Path);
            // }
        let relativePath = localUrl.Replace ('/', '\\')
        let relativePath = 
            if relativePath.StartsWith @"\" then
                relativePath.Substring 1
            else
                relativePath

        // let localFile =
        //     try
        //         Path.Combine (rootDirectory, relativePath)
        //     with 
        //     | e -> 
        //         request.categoryLogger.log LogLevel.Trace (sprintf "Invalid path: %s, %A , {e}" path e)
        //         failwith InvalidPathException 
            

        ()