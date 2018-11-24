namespace WebServer

open System

type HeaderKey =
    Method = 0
    | Path = 1
    | HttpVersion = 2
    | IfModifiedSince = 3
    | StatusOK = 4
    | Status404 = 5
    | Status301 = 6
    | Status304 = 7
    | Status500 = 8
    | ContentLength = 9
    | ContentType = 10
    | ContentEncoding = 11
    | AcceptEncoding = 12
    | Date = 13
    | Server = 14
    | XFrameOptions = 15 
    | Location = 16
    | Expires = 17
    | LastModified = 18
    | CacheControl = 19
    | Accept = 20

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

module Header = 
    let getHttpVersionAsString httpVersion =
        match httpVersion with
        | HttpVersion.Http1 -> "HTTP/1.0"
        | HttpVersion.Http11 -> "HTTP/1.1"
        | HttpVersion.Http2 -> "HTTP/2"
        | _ -> "HTTP??"
