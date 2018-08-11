namespace WebServer

module Server =
    open Microsoft.Extensions.Logging
    
    let Start (configuration: Configuration) = 
        Logger.Info "Starting Web Server"
        
        Logger.Info "Web Server started"
        ()

    let Stop () =
        ()
