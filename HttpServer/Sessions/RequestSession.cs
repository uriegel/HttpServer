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

        public static Counter Instances { get; } = new Counter();

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

        public async Task SendHtmlStringAsync(string html)
        {
            var bytes = Encoding.UTF8.GetBytes(html);
            //responseHeaders.Add("Content-Length", $"{bytes.Length}");
            //responseHeaders.Add("Content-Type", "text/html; charset=UTF-8");
            //var headerBuffer = responseHeaders.Access(SocketSession.UseTls, HttpResponseString, Headers);
            //await WriteAsync(headerBuffer, 0, headerBuffer.Length);
            //await WriteAsync(bytes, 0, bytes.Length);
        }

    }
}
