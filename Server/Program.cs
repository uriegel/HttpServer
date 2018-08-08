using System;
using HttpServer;
using HttpServer.HPack;
using static HttpServer.HPack.HuffmanTree;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var mist = Http2.HuffmanTree.huffmanTree;
            Console.WriteLine("");

            void ToString(Node node, string path = "")
            {
                if (node.Node0 != null)
                    ToString(node.Node0, $"{path}0");
                if (node.Node1 != null)
                    ToString(node.Node1, $"{path}1");
                if (node.Value.HasValue)
                    Console.WriteLine($"{path}-{node.Value}");
            }

            ToString(HuffmanTree.Root);

            var encoded = new byte[] { 0x20, 0xd0, 0xb1, 0x7c, 0x2c, 0x41, 0x95, 0x03, 0xb1, 0x66, 0x57, 0xb6, 0x31, 0x2e, 0x41, 0x95, 0x7a, 0x0e, 0x41, 0xd1, 0x71, 0xe0, 0x3c, 0x0f };
            var watt = HuffmanTree.Decode(encoded);

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
