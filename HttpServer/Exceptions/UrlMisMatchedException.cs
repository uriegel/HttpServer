using System;
using System.Collections.Generic;
using System.Text;

namespace HttpServer.Exceptions
{
    class UrlMismatchException : Exception
    {
        public UrlMismatchException() : base("Url mismatch") { }
    }
}
