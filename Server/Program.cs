using System;
using HttpServer;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = new Configuration
            {
                Webroot = @"..\..\..\..\..\SuperfitUI\dist\superfitui",
                IsTlsEnabled = true
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
