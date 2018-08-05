using System;
using System.Collections.Generic;
using System.Text;
using HttpServer.Enums;

namespace HttpServer.Http2
{
    class Headers : Frame
    {
        public HeadersFlags Flags { get => (HeadersFlags)flags; }

        public Headers(int length, Type type, HeadersFlags flags, long streamId, byte[] payload)
            : base(length, type, (byte)flags, streamId, payload)
        {
        }
}
}
