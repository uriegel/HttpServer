namespace WebServer

open System.Net

type Request = {
    remoteEndPoint: IPEndPoint
    isSecure: bool
    categoryLogger: CategoryLogger
    header: HeaderKey->obj
}
