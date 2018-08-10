using System;
using System.Collections.Generic;
using System.Text;
using HttpServer.Enums;

namespace HttpServer.Http22
{
    class Frame
    {
        public const int Size = 9;

        public int Length
        {
            get
            {
                var bytes = new byte[4];
                bytes[0] = header[2];
                bytes[1] = header[1];
                bytes[2] = header[0];
                return BitConverter.ToInt32(bytes);
            }
        }

        public FrameType Type { get => (FrameType)header[3]; }

        public byte RawFlags { get => header[4]; }

        public int StreamId
        {
            get
            {
                var bytes = new byte[4];
                bytes[0] = header[8];
                bytes[1] = header[7];
                bytes[2] = header[6];
                bytes[3] = (byte)(header[5] & ~0x1);
                return BitConverter.ToInt32(bytes);
            }
        }

        public Frame(byte[] header, byte[] payload)
        {
            this.header = header;
            this.payload = payload;
        }

        public byte[] Serialize()
        {
            var result = new byte[header.Length + (payload?.Length ?? 0)];
            Array.Copy(header, result, header.Length);
            if (payload != null)
                Array.Copy(payload, header.Length, result, 0, payload.Length);
            return result;
        }

        protected readonly byte[] header;
        protected readonly byte[] payload;
    }
}
