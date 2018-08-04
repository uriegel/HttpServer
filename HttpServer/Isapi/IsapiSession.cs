using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer.Enums;
using HttpServer.Headers;
using HttpServer.Interfaces;

namespace HttpServer.Isapi
{
    class IsapiSession
    {
        public static Counter Instances { get; } = new Counter();

        public bool Request(ISession session, Method method, string contentType, string localFile, string pathInfo, string queryString)
            => throw new NotImplementedException();

        public void SetInfo(ServerResponseHeaders responseHeaders)
            => responseHeaders.SetInfo(statusCode, contentLength);

        int statusCode;
        int contentLength;
    }
}
