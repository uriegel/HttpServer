using System;
using System.Collections.Generic;
using System.Text;

namespace HttpServer.HPack
{
    static class Globals
    {
        /// <summary>
        /// Maximium header size allowed
        /// </summary>
        public const int HTTP_MAX_HEADER_SIZE = 80 * 1024;
    }
}
