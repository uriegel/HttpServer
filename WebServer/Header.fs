namespace WebServer

open System

type HeaderKey =
    Method = 0
    | Path = 1
    | IfModifiedSince = 2

type IHeader = 
    abstract GetHeaderValue: HeaderKey->Object
