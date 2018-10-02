namespace WebServer

open System
open System.Text.RegularExpressions

type UrlQueryType = 
    {
        method: string
        rawParameters: Map<string,string>
    }
    member this.Query key = this.rawParameters.TryFind key    

module UrlQuery = 
    let private urlParameterRegex = Regex (@"(?<key>[^&?]*?)=(?<value>[^&?]*)", RegexOptions.Compiled)

    let create (url: string) =
        if url.Contains "?" then
            let urlParameterString = Uri.UnescapeDataString url
            let matches = urlParameterRegex.Matches urlParameterString
        
            let unescapeSpaces (uri: string) = uri.Replace ('+', ' ')
            let methodPath = url.Substring (0, url.IndexOf('?'))
            let pos = methodPath.LastIndexOf '/' + 1 
            {
                method = methodPath.Substring pos
                rawParameters = matches 
                                |> Seq.cast 
                                |> Seq.map (fun (s: Match) -> (s.Groups.["key"].Value, Uri.UnescapeDataString (unescapeSpaces s.Groups.["value"].Value))) 
                                |> Map.ofSeq
            }
        else 
            let pos = url.LastIndexOf '/' + 1 
            {
                method = url.Substring (pos)
                rawParameters = Map.empty
            }