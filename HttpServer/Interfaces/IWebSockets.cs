using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Interfaces
{
    public interface IWebSocket
    {
        bool IsSecureConnection { get; }
        IPEndPoint LocalEndPoint { get; }
        IPEndPoint RemoteEndPoint { get; }
        string[] Protocols { get; }
        string UserAgent { get; }

        Task SendAsync(string payload);
        Task SendJsonAsync(object jsonObject);
        void Initialize(Func<string, Task> onMessage, Func<Task> onClosed);
        void Close();
    }
}
