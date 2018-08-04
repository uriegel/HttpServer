using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Interfaces
{
    public interface IWebSocketInternalSession
    {
        Task SendPongAsync(string payload);
    }
}
