namespace WebServer

open System.Net

type SocketSession = {
    id: int
    remoteEndPoint: IPEndPoint
    isSecure: bool
}