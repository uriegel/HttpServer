using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HttpServer.Headers;
using HttpServer.Http2;
using HttpServer.Interfaces;

namespace HttpServer.Sessions
{
    class RequestSession2 : RequestSession
    {
        public RequestSession2(Server server, SocketSession socketSession, Stream networkStream)
            : base(server, socketSession, networkStream)
        {
        }

        public override async Task<bool> StartAsync()
        {
            var bytes = new byte[24];
            var read = await networkStream.ReadAsync(bytes, 0, bytes.Length);
            if (read != bytes.Length)
                return false;
            var frame = await ReadFrameAsync();

            if (frame.Type != Http2.Type.SETTINGS)
                throw new Exception("No Settings");

            read = await networkStream.ReadAsync(bytes, 0, bytes.Length);

            return false;
        }

        async Task<Frame> ReadFrameAsync()
        {
            async Task<byte[]> ReadAsync(int count)
            {
                var bytes = new byte[count];
                var read = 0;
                do
                {
                    read += await networkStream.ReadAsync(bytes, read, bytes.Length - read);
                }
                while (read < bytes.Length);
                return bytes;
            }

            var header = await ReadAsync(Frame.Size);
            var lengthBytes = new byte[4];
            lengthBytes[0] = header[2];
            lengthBytes[1] = header[1];
            lengthBytes[3] = header[0];
            var length = BitConverter.ToInt32(lengthBytes);
            var type = (Http2.Type)header[3];
            var flags = header[4];
            var streamId = BitConverter.ToInt32(header, 5);
            var payload = await ReadAsync(length);
            switch (type)
            {
                case Http2.Type.SETTINGS:
                    return new Settings(length, type, flags, streamId, payload);
                default:
                    throw new Exception("Frame type not supported");
            }
        }
        
        const string check = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n";
    }
}
