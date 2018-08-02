using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer.WebSockets;

namespace HttpServer.Interfaces
{
    interface IWebSocketsConsumer
    {
        void OnNew(WebSocket webSocket, string query);
    }
}
