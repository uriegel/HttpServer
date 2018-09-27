namespace WebServer

type RequestHeaders = {
    Method: Method
    Path: string
    HttpVersion: HttpVersion
    AcceptEncoding: ContentEncoding
}