namespace WebServer

type Request = {
    categoryLogger: CategoryLogger
    header: RequestHeaders
    asyncSendBytes: ResponseHeaderValue[]->byte[] option->Async<unit> 
}
