using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace HttpServer
{
    static class Constants
    {
        public const string NotModified = "Fri, 01 Jun 2012 08:28:30 GMT";
        public static string Server { get; }

        static Constants()
            => Server = "CAESAR Web Server/" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion.ToString();
    }
}
