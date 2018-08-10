using System;
using System.IO;
using HttpServer;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var strom = File.OpenRead(@"..\..\..\header.txt"))
            {
                var bytes = new byte[strom.Length];
                strom.Read(bytes, 0, bytes.Length);
                var result = Http2.HPack.Decode(new MemoryStream(bytes));
            }

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
