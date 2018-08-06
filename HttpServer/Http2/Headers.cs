using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HttpServer.Enums;

namespace HttpServer.Http2
{
    class Headers : Frame
    {
        public HeadersFlags Flags { get => (HeadersFlags)RawFlags; }
        public byte PadLength { get => payload[0]; }
        public bool E { get => (payload[1] & 1) != 0; }
        public byte[] Payload { get => payload; }
        
        public int StreamDependency
        {
            get
            {
                var value = new byte[4];
                value[0] = payload[4];
                value[1] = payload[3];
                value[2] = payload[2];
                value[3] = (byte)(payload[1] & ~0x1);
                return BitConverter.ToInt32(value);
            }
        }

        public byte Weight { get => payload[5]; }

        public Headers(byte[] header, byte[] payload)
            : base(header, payload)
        {
            //try
            //{
            //    //using (var strom = File.OpenWrite(@"d:\affe.txt"))
            //    //    strom.Write(payload, 6, payload.Length - 6);
            //}
            //catch (Exception e)
            //{

            //}
        }
    }
}
