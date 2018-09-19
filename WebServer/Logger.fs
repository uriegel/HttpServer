namespace WebServer

open Microsoft.Extensions.Logging

module Logger =

    let mutable lowTraceEnabled = false
    let private loggerFactory = new LoggerFactory ()
    loggerFactory.AddProvider (new ConsoleLoggerProvider ())
    //loggerFactory.AddDebug () |> ignore
    let private logger = loggerFactory.CreateLogger("WebServer")
    
    let log category logLevel text =
        logger.Log (logLevel, sprintf "%s-%s" category text)

    let lowTrace category getText = if lowTraceEnabled then logger.LogTrace (sprintf "%s-%s" category (getText ()))
