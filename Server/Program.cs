using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using HttpServer;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var certificateFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "certificate.pfx");
            var beits = new byte[new FileInfo(certificateFile).Length];
            using (var file = File.OpenRead(certificateFile))
                file.Read(beits, 0, beits.Length);
            var certificate = new X509Certificate2(beits, "uwe");

            WebServer.Logger.lowTraceEnabled = true;
            var newConfig = new WebServer.InitializationData
            {
                //Webroot = @"..\..\..\..\..\SuperfitUI\dist\superfitui",
                Webroot = @"C:\Program Files\caesar\CAEWebSrv\web",
                IsTlsEnabled = true,
                //TlsPort = 4433,
                //Http2 = true,
                CertificateName = "CAESAR",
                //DomainName = "riegel.selfhost.eu",
                DomainName = "cas-ws121013.caseris.intern",
                TlsRedirect = true
            };
            WebServer.Server.Start(newConfig);
            Console.WriteLine("Press any key to stop...");
            Console.ReadLine();
            WebServer.Server.Stop();

            // C#

//             var configuration = new Configuration
//             {
//                 //Webroot = @"..\..\..\..\..\SuperfitUI\dist\superfitui",
// //                Webroot = @"C:\Program Files\caesar\CAEWebSrv\web",
//                 IsTlsEnabled = true,
//                 TlsPort = 443,
//                 Port = 80,
// //                Certificate = certificate,
//   //              HTTP2 = true
//             };
//             var server = new HttpServer.Server(configuration);
//             server.Start();
//             Console.WriteLine("Press any key to stop...");
//             Console.ReadLine();
//             server.Stop();
//             Console.ReadLine();
        }
    }
}
