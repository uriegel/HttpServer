using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer.WebSockets;

namespace HttpServer.Sessions
{
    class RequestSession
    {
        public Server Server { get; private set; }

        public Service GetServiceInfo()
            => throw new NotImplementedException();

        public bool CheckWsUpgrade() => throw new NotImplementedException();

        public Task<WebSocketSession> UpgradeWebSocketAsync() => throw new NotImplementedException();
    }
}
