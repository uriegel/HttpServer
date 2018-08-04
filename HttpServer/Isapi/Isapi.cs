using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Isapi
{
    public class Isapi
    {
        public string Url { get; private set; }

        public void Terminate() { }

        internal IsapiSession CreateRequest()
        {
            return null;
        }
    }
}
