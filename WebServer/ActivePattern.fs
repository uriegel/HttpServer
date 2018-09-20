namespace WebServer
open System

module ActivePatterns = 
    let (|InvariantEqual|_|) text arg = 
        if String.Compare(text, arg, StringComparison.OrdinalIgnoreCase) = 0 then Some() else None

    let (|SplitChar|_|) (chr: char) (arg: string) = 
        let index = arg.IndexOf chr
        match index with
            | -1 -> None
            | _ -> Some (arg.Substring (0, index), arg.Substring (index + 1))