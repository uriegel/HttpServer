using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HttpServer.Exceptions;

namespace HttpServer.WebSockets
{
    class WsDecodedStream : Stream
    {
        #region Properties	

        public int DataPosition { get; protected set; }
        public string Payload { get; protected set; }

        #endregion

        #region Constructor	

        public WsDecodedStream(Stream stream, int length, byte[] key, bool encode)
        {
            this.stream = stream;
            this.length = length;
            this.key = key;
            this.encode = encode;
            buffer = new byte[length];
            ReadStream(0);
        }

        protected WsDecodedStream()
        {
        }

        #endregion

        #region Stream	

        public override bool CanRead { get { return true; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return false; } }

        public override long Length { get { return length - DataPosition; } }

        public override long Position
        {
            get { return _Position; }
            set
            {
                if (value > Length)
                    throw new IndexOutOfRangeException();
                _Position = value;
            }
        }
        long _Position;

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Position + count > length - DataPosition)
                count = (int)length - DataPosition - (int)Position;
            if (count == 0)
                return 0;

            Array.Copy(this.buffer, offset + DataPosition + Position, buffer, offset, count);
            Position += count;

            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
            => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotImplementedException();

        public virtual int WriteHeaderToAnswer(byte[] bytes, int position)
        {
            Array.Copy(buffer, 0, bytes, position, DataPosition);
            return DataPosition;
        }

        void ReadStream(int position)
        {
            var read = 0;
            while (read < length - position)
            {
                var newlyRead = stream.Read(buffer, read + position, (int)length - position - read);
                if (newlyRead == 0)
                    throw new ConnectionClosedException();
                read += newlyRead;
            }

            if (encode)
            {
                for (var i = 0; i < length - position; i++)
                {
                    buffer[i + position] = (Byte)(buffer[i + position] ^ key[i % 4]);
                }
            }

            if (position == 0)
            {
                Payload = Encoding.UTF8.GetString(buffer, 0, (int)length);
                DataPosition = Payload.Length + 1;
            }
        }

        #endregion

        public void AddContinuation(int length, byte[] key, bool encode)
        {
            var oldLength = buffer.Length;
            Array.Resize<byte>(ref buffer, oldLength + length);
            this.key = key;
            this.encode = encode;
            this.length += length;
            ReadStream(oldLength);
        }

        #region Fields

        Stream stream;
        byte[] buffer;
        long length;
        byte[] key;
        bool encode;

        #endregion
    }
}
