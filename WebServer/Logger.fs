namespace WebServer

open Microsoft.Extensions.Logging

module Logger =

    let mutable lowTraceEnabled = false
    let internal loggerFactory = new LoggerFactory()
    loggerFactory.AddProvider(new ConsoleLoggerProvider())
    loggerFactory.AddDebug() |> ignore
    let internal logger = loggerFactory.CreateLogger("WebServer")
    
    let LowTrace (getText: unit->string) = if lowTraceEnabled then logger.LogTrace (getText ())
    let Trace text = logger.LogTrace text
    let Info text = logger.LogInformation text
    let Warning text = logger.LogWarning text
    let Error text = logger.LogError text
