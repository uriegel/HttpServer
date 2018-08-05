using System;
using System.Collections.Generic;
using System.Text;

namespace HttpServer.Http2
{
    enum Type : byte
    {
        /// <summary>
        /// Carries the core content for a stream
        /// </summary>
        DATA = 0x0,
        /// <summary>
        /// Contains the HTTP headers and, optionally, priorities
        /// </summary>
        HEADERS = 0x1,
        /// <summary>
        /// Indicates or changes the stream priority and dependencies
        /// </summary>
        PRIORITY = 0x2,
        /// <summary>
        ///  Allows an endpoint to end a stream (generally an error case)
        /// </summary>
        RST_STREAM = 0x3,
        /// <summary>
        /// Communicates connection-level parameters
        /// </summary>
        SETTINGS = 0x4,
        /// <summary>
        /// Indicates to a client that a server is about to send something
        /// </summary>
        PUSH_PROMISE = 0x5,
        /// <summary>
        /// Tests connectivity and measures round-trip time (RTT)
        /// </summary>
        PING = 0x6,
        /// <summary>
        /// Tells an endpoint that the peer is done accepting new streams
        /// </summary>
        GOAWAY = 0x7,
        /// <summary>
        /// Communicates how many bytes an endpoint is willing to receive(used for flow control)
        /// </summary>
        WINDOW_UPDATE = 0x8,
        /// <summary>
        /// Used to extend HEADER blocks
        /// </summary>
        CONTINUATION = 0x9 
    }
}
