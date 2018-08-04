using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HttpServer.Interfaces;

namespace HttpServer.WebSockets
{
    class WebSocket : IWebSocket
    {
        #region IWebSocket

        public string[] Protocols { get => webSocketSession.Protocols; }

        public bool IsSecureConnection { get => webSocketSession.IsSecureConnection; }
        public IPEndPoint LocalEndPoint { get => webSocketSession.LocalEndPoint; }
        public IPEndPoint RemoteEndPoint { get => webSocketSession.RemoteEndPoint; }
        public string UserAgent { get => webSocketSession.UserAgent; }

        public Task SendAsync(string payload) => webSocketSession.SendAsync(payload);

        public Task SendJsonAsync(object jsonObject) => webSocketSession.SendJsonAsync(jsonObject);

        public void Initialize(Func<string, Task> onMessage, Func<Task> onClosed)
        {
            webSocketSession.Initialize(onMessage, onClosed);
            webSocketSession.StartMessageReceiving();
        }

        public void Close()
            => webSocketSession.Close();

        #endregion

        public WebSocket(WebSocketSession webSocketSession) => this.webSocketSession = webSocketSession;

        readonly WebSocketSession webSocketSession;
    }
}
