using System;
using System.IO;
using HttpServer;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var newCofig = new WebServer.InitializationData
            {
                Webroot = @"..\..\..\..\..\SuperfitUI\dist\superfitui",
                // Webroot = @"C:\Program Files\caesar\CAEWebSrv\web",
                IsTlsEnabled = true,
                TlsRedirect = true
            };
            WebServer.Server.Start(newCofig);
            WebServer.Server.Stop();

            Logger.Current.MinLogLevel = Logger.LogLevel.LowTrace;
            var configuration = new Configuration
            {
//                Webroot = @"..\..\..\..\..\SuperfitUI\dist\superfitui",
                Webroot = @"C:\Program Files\caesar\CAEWebSrv\web",
                IsTlsEnabled = true,
                HTTP2 = true
            };
            var server = new HttpServer.Server(configuration);
            server.Start();
            Console.WriteLine("Press any key to stop...");
            Console.ReadLine();
            server.Stop();
            Console.ReadLine();
        }
    }
}
