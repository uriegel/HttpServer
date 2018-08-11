namespace WebServer

open Microsoft.Extensions.Logging

module Logger =

    let internal loggerFactory = new LoggerFactory()
    loggerFactory.AddDebug() |> ignore
    loggerFactory.AddConsole() |> ignore
    let internal logger = loggerFactory.CreateLogger("WebServer")
    
    let Info text = logger.LogInformation text
    let Trace text = logger.LogTrace text
    let Warning text = logger.LogWarning text
