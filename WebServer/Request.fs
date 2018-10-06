namespace WebServer

type Request = {
    socketSessionId: int
    categoryLogger: CategoryLogger
    header: RequestHeaders
    asyncSendBytes: (ResponseHeaderValue list)->byte[] option->Async<unit> 
    asyncSendRaw: byte[]->Async<unit> 
}
