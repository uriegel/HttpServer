﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.WebSockets
{
    class WebSocketSession
    {
        public static Counter Instances { get; } = new Counter();
    }
}
