namespace WebServer

type Request = {
    categoryLogger: CategoryLogger
    header: RequestHeaders
    asyncSendBytes: Request->ResponseHeaderValue[]->byte[] option->Async<unit> 
}
