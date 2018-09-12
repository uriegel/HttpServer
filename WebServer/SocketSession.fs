namespace WebServer

open System.Net.Sockets
open System.Threading

module SocketSession = 
    let mutable private idSeed = 0
        
    let startReceiving (tcpClient: TcpClient) = 
        let id = Interlocked.Increment &idSeed
        Logger.LowTrace(fun () -> sprintf "%d - New %ssocket session created: - %A" id (if Settings.Current.IsTlsEnabled then "secure " else "") tcpClient.Client.RemoteEndPoint)
        tcpClient.ReceiveTimeout <- Settings.Current.SocketTimeout
        tcpClient.SendTimeout <- Settings.Current.SocketTimeout
        
        let startReceiving () = 
            ()
        ()
            

