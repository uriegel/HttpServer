using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = new Configuration();
            var server = new HttpServer.Server(configuration);

        }
    }
}
