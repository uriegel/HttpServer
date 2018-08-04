using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using HttpServer.Interfaces;

namespace HttpServer.WebSockets
{
    class WebSocketProxy
    {
        #region Properties	

        public IWebSocket Client { get; private set; }
        public string Target { get; private set; }

        #endregion

        #region Constructor	

        public WebSocketProxy(IWebSocket webSocket, string target)
        {
            Logger.Current.Info($"New web socket proxy connection from {webSocket.RemoteEndPoint} to {target}");
            Target = target;

            proxyServer = new Client(target, OnMessageFromServerAsync, OnServerClosed);
            proxyServer.OpenAsync().Wait();

            Client = webSocket;
            Client.Initialize(OnMessageFromClient, OnClientClosed);
        }

        #endregion

        #region Methods

        protected Task OnMessageFromClient(string payload)
        {
            Logger.Current.Trace($"Message from {Client?.RemoteEndPoint}: {(payload.Length > 512 ? payload.Substring(512) + "..." : payload)}");
            return proxyServer?.SendAsync(payload);
        }

        protected Task OnMessageFromServerAsync(string payload)
        {
            Logger.Current.Trace($"Message to {Client?.RemoteEndPoint}: {(payload.Length > 512 ? payload.Substring(512) + "..." : payload)}");
            return Client?.SendAsync(payload);
        }

        protected Task OnClientClosed()
        {
            if (null != proxyServer)
            {
                Logger.Current.Info($"Client closed connection: {Client?.RemoteEndPoint}");
                proxyServer.Close();
                proxyServer = null;

                OnClosed();
            }
            return Task.FromResult(0);
        }

        protected void OnServerClosed()
        {
            if (null != Client)
            {
                Logger.Current.Info($"Server closed connection, disconnecting: {Client.RemoteEndPoint}");
                Client.Close();
                Client = null;

                OnClosed();
            }
        }

        protected virtual void OnClosed()
        {
        }
        
        #endregion

        #region Fields
        Client proxyServer;
        
        #endregion
    }
}
