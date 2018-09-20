namespace WebServer

open System.Net

type SocketSession = {
    remoteEndPoint: IPEndPoint
    isSecure: bool
}