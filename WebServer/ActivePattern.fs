namespace WebServer
open System

module ActivePatterns = 
    let (|InvariantEqual|_|) text arg = 
        if String.Compare(text, arg, StringComparison.OrdinalIgnoreCase) = 0 then Some() else None