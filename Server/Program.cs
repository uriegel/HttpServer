using System;
using System.IO;
using HttpServer;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            WebServer.Logger.lowTraceEnabled = true;
            var newCofig = new WebServer.InitializationData
            {
                Webroot = @"..\..\..\..\..\SuperfitUI\dist\superfitui",
                //Webroot = @"C:\Program Files\caesar\CAEWebSrv\web",
                IsTlsEnabled = true,
                Http2 = true,
                //CertificateName = "CAESAR",
                CertificateName = "URIEGEL",
                DomainName = "riegel.selfhost.eu",
                //DomainName = "cas-ws121013.caseris.intern",
                TlsRedirect = true
            };
            WebServer.Server.Start(newCofig);
            Console.WriteLine("Press any key to stop...");
            Console.ReadLine();
            WebServer.Server.Stop();


            Console.ReadLine();
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
