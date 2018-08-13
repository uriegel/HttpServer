namespace WebServer

open Microsoft.Extensions.Logging

module Logger =

    let internal loggerFactory = new LoggerFactory()
    loggerFactory.AddProvider(new ConsoleLoggerProvider())
    loggerFactory.AddDebug() |> ignore
    let internal logger = loggerFactory.CreateLogger("WebServer")
    
    let Info text = logger.LogInformation text
    let Trace text = logger.LogTrace text
    let Warning text = logger.LogWarning text
    let Error text = logger.LogError text
