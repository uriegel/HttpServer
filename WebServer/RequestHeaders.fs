namespace WebServer
open System

type RequestHeaders = {
    method: Method
    path: string
    httpVersion: HttpVersion
    acceptEncoding: ContentEncoding
    ifModifiedSince: DateTime option
    getValue: HeaderKey->(string option)
}