namespace WebServer
open System.Net
open System.Net.Sockets

module Ipv6TcpListenerFactory =

    type Ipv6Listener = {
        Listener: TcpListener 
        Ipv6: bool
    }

    let private setDualMode (socket: Socket) =
        socket.SetSocketOption (SocketOptionLevel.IPv6, LanguagePrimitives.EnumOfValue<int, SocketOptionName> 27, 0)
    
    let create port = 
        try
            let result = {
                Listener = TcpListener (IPAddress.IPv6Any, port)
                Ipv6 = true
            }
            setDualMode result.Listener.Server
            result
        with 
        | :? SocketException as se when se.SocketErrorCode <> SocketError.AddressFamilyNotSupported ->
            raise se
        | :? SocketException ->
            let result = {
                Listener = TcpListener (IPAddress.Any, port)
                Ipv6 = false
            }
            result
