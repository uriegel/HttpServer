namespace WebServer

open System

type HeaderKey =
    Method = 0
    | Path = 1
    | IfModifiedSince = 2

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

