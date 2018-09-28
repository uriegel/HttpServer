namespace WebServer
open System

type RequestHeaders = {
    Method: Method
    Path: string
    HttpVersion: HttpVersion
    AcceptEncoding: ContentEncoding
    IfModifiedSince: DateTime option
}