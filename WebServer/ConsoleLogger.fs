namespace WebServer

open System
open System.Collections.Concurrent
open System.Threading
open Microsoft.Extensions.Logging

type LogItem = {
    LogLevel: LogLevel
    text: string
}

type ConsoleLogger() = 
    let blockingCollection = new BlockingCollection<LogItem> ()
    let thread = Thread (fun () -> 
        while not blockingCollection.IsCompleted do
            let item = blockingCollection.Take ()
            let recentColor = Console.ForegroundColor 
            Console.ForegroundColor <- 
                match item.LogLevel with
                | LogLevel.Critical -> ConsoleColor.DarkRed
                | LogLevel.Error -> ConsoleColor.DarkRed
                | LogLevel.Warning -> ConsoleColor.DarkYellow
                | LogLevel.Information -> ConsoleColor.DarkGreen
                | _ -> recentColor
            Console.WriteLine item.text
            Console.ForegroundColor <- recentColor
    )
    do
        thread.IsBackground <- true
        thread.Start ()
    
    interface ILogger with
        member this.BeginScope (state: 'TState): System.IDisposable = null
        member this.Log (logLevel: LogLevel, eventId: EventId, state: 'TState, e: exn, formatter: System.Func<'TState,exn,string>) = 
            blockingCollection.Add ({ LogLevel = logLevel; text = formatter.Invoke (state, e)})
        member this.IsEnabled (logLevel: LogLevel) = true

type ConsoleLoggerProvider() =
    interface ILoggerProvider with
        member this.CreateLogger(categoryName: string) = 
            ConsoleLogger () :> ILogger
        member this.Dispose(): unit = 
            ()
