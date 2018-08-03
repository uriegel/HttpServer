using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Sessions
{
    public class SocketSession
    {
        public static Counter Instances { get; } = new Counter();

        public static async void StartReceiving(Server server, TcpClient tcpClient, bool isSecured) { }
    }
}
