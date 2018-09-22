namespace WebServer

open System

type HeaderKey =
    Method = 0
    | Path = 1
    | HttpVersion = 2
    | IfModifiedSince = 3
    | Status404 = 4
    | ContentLength = 5
    | ContentType = 6

type HttpVersion = 
    | Http1 = 0
    | Http11 = 1
    | Http2 = 2

type Method =
    Get = 0
    | Post = 1
    | Put = 2
    | Delete = 3
    | Head = 4
    | Options = 5
    | Connect = 6
    | Trace = 7
    | Patch = 8

type ResponseHeaderValue = {
    key: HeaderKey
    value: obj option
}
