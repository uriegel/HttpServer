using System;
using System.Collections.Generic;
using System.Text;

namespace HttpServer.Interfaces
{
    interface IHeaders
    {
        string ContentType { get; }
        string this[string key] { get; }
        IEnumerable<KeyValuePair<string, KeyValuePair<string, string>>> Raw { get; }
    }
}
