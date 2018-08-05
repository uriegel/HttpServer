using System;
using System.Collections.Generic;
using System.Text;

namespace HttpServer.Http2
{
    class Frame
    {
        public const int Size = 9;

        public Frame(int length, Type type, byte flags, long streamId, byte[] payload)
        {
            Length = length;
            Type = type;
            this.flags = flags;
            StreamId = streamId;
            Payload = payload;
        }

        public byte[] Serialize()
        {
            if (Length == 0)
            {
                var result = new byte[Size];
                result[3] = (byte)Type;
                result[4] = flags;
                //result[5] = StreamId;
                return result;
            }
            else
                throw new NotImplementedException();
        }

        public readonly int Length;
        public readonly Type Type;
        public readonly long StreamId;
        protected readonly byte flags;
        readonly byte[] Payload;
    }
}
