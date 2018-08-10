using System;
using System.Collections.Generic;
using System.Text;

namespace HttpServer.Http22
{
    class WindowUpdate : Frame
    {
        public int SizeIncrement
        {
            get
            {
                var value = new byte[4];
                value[0] = payload[3];
                value[1] = payload[2];
                value[2] = payload[1];
                value[3] = (byte)(payload[0] & ~0x1);
                return BitConverter.ToInt32(value);
            }
        }

        public WindowUpdate(byte[] header, byte[] payload)
            : base(header, payload)
        {
        }
    }
}
