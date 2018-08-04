using System;
using System.Collections.Generic;
using System.Text;
using HttpServer.Enums;

namespace HttpServer.Interfaces
{
    interface IRequestHeaders : IHeaders
    {
        Method Method { get; }
        string Url { get; }
        string Host { get; }
        string Http { get; }
        ContentEncoding ContentEncoding { get; }
        bool Http10 { get; }
        string UserAgent { get; }
    }
}
