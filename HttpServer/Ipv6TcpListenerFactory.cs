using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using HttpServer.Extensions;

namespace HttpServer
{
    public static class Ipv6TcpListenerFactory
    {
        public struct Ipv6Listener
        {
            public TcpListener Listener;
            public bool Ipv6;
        }

        public static Ipv6Listener Create(int port)
        {
            var result = new Ipv6Listener();
            try
            {
                result.Listener = new TcpListener(IPAddress.IPv6Any, port);
                result.Listener.Server.SetDualMode();
                result.Ipv6 = true;
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode != SocketError.AddressFamilyNotSupported)
                    throw;
                result.Listener = new TcpListener(IPAddress.Any, port);
                result.Ipv6 = false;
            }
            return result;
        }
    }
}
