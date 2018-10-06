namespace WebServer

type RequestData = {
    socketSessionId: int
    header: RequestHeaders
}

type Request = {
    data: RequestData
    categoryLogger: CategoryLogger
    asyncSendBytes: (ResponseHeaderValue list)->byte[] option->Async<unit> 
    asyncSendRaw: byte[]->Async<unit> 
}

type SseContext = {
    request: RequestData
    send: string->string->unit
}