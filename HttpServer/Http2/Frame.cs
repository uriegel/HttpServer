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
            Flags = flags;
            StreamId = streamId;
            Payload = payload;
        }

        public readonly int Length;
        public readonly Type Type;
        public readonly byte Flags;
        public readonly long StreamId;
        readonly byte[] Payload;
    }
}
