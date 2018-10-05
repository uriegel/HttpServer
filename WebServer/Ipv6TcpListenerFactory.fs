namespace WebServer
open System.Net
open System.Net.Sockets

module Ipv6TcpListenerFactory =

    type Ipv6Listener = {
        listener: TcpListener 
        ipv6: bool
    }

    let private setDualMode (socket: Socket) =
        socket.SetSocketOption (SocketOptionLevel.IPv6, LanguagePrimitives.EnumOfValue<int, SocketOptionName> 27, 0)
    
    let create port = 
        try
            let result = {
                listener = TcpListener (IPAddress.IPv6Any, port)
                ipv6 = true
            }
            setDualMode result.listener.Server
            result
        with 
        | :? SocketException as se when se.SocketErrorCode <> SocketError.AddressFamilyNotSupported ->
            raise se
        | :? SocketException ->
            let result = {
                listener = TcpListener (IPAddress.Any, port)
                ipv6 = false
            }
            result
