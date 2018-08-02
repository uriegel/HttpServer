using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer.Interfaces;
using HttpServer.WebSockets;

namespace HttpServer.Sessions
{
    class RequestSession : ISession
    {
        public string Id { get; }

        public Server Server { get; private set; }

        public Service GetServiceInfo()
            => throw new NotImplementedException();

        public bool CheckWsUpgrade() => throw new NotImplementedException();

        public Task<WebSocketSession> UpgradeWebSocketAsync() => throw new NotImplementedException();
         
        public Task SendExceptionAsync(Exception e) => throw new NotImplementedException();

        public void Close() => Close(false);

        public void Close(bool fullClose)
        {
            try
            {
                if (fullClose)
                {
                    //networkStream.Close();
                    //isClosed = true;
                }
                //else
                    // SocketSession.Client.Client.Shutdown(SocketShutdown.Send);
            }
            catch { }
        }

    }
}
