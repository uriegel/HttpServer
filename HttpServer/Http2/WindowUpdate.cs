using System;
using System.Collections.Generic;
using System.Text;

namespace HttpServer.Http2
{
    class WindowUpdate : Frame
    {
        public readonly int SizeIncrement;

        public WindowUpdate(int length, Type type, byte flags, long streamId, byte[] payload)
            : base(length, type, flags, streamId, payload)
        {
            var value = new byte[4];
            value[0] = payload[3];
            value[1] = payload[2];
            value[2] = payload[1];
            value[3] = payload[0];
            SizeIncrement = BitConverter.ToInt32(value);
        }
    }
}
