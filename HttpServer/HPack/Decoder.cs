using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HttpServer.HPack
{
    class Decoder
    {
        public Decoder(Http2.Headers headers, int maxHeaderTableSize)
        {
            this.maxHeaderTableSize = maxHeaderTableSize;
            this.headers = headers;
            var padding = ((headers.Flags & Enums.HeadersFlags.PADDED) == Enums.HeadersFlags.PADDED) ? headers.PadLength : (byte)0;
            headerReader = new BinaryReader(new MemoryStream(headers.Payload, 6, headers.Payload.Length - 6 - padding));
        }

        public void Decode()
        {
            while (headerReader.BaseStream.Length - headerReader.BaseStream.Position > 0)
            {

            }
        }

        readonly BinaryReader headerReader;
        readonly Http2.Headers headers;
        readonly int maxHeaderTableSize;
    }
}
