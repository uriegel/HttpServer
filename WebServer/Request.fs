namespace WebServer

type Request = {
    categoryLogger: CategoryLogger
    header: HeaderKey->obj
    asyncSendBytes: ResponseHeaderValue[]->byte[]->Async<unit>
}
