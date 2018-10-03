namespace WebServer

type Request = {
    categoryLogger: CategoryLogger
    header: RequestHeaders
    asyncSendBytes: (ResponseHeaderValue list)->byte[] option->Async<unit> 
    asyncSendRaw: byte[]->Async<unit> 
}
