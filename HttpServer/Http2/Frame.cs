using System;
using System.Collections.Generic;
using System.Text;

namespace HttpServer.Http2
{
    class Frame
    {
        public const int Size = 9;

        public enum Flags : byte
        {
            NotSet = 0x0,
            Ack = 0x1
        }

        public Frame(int length, Type type, byte flag, long streamId, byte[] payload)
        {
            Length = length;
            Type = type;
            Flag = (Flags)flag;
            StreamId = streamId;
            Payload = payload;
        }

        public readonly int Length;
        public readonly Type Type;
        public readonly Flags Flag;
        public readonly long StreamId;
        readonly byte[] Payload;
    }
}
