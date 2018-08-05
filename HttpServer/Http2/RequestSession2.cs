using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HttpServer.Enums;
using HttpServer.Headers;
using HttpServer.Http2;
using HttpServer.Interfaces;
using HttpServer.Sessions;

namespace HttpServer.Http2
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

            do
            {
            }
            while (await NextFrameAsync());
            return false;
        }

        async Task<bool> NextFrameAsync()
        {
            var frame = await ReadFrameAsync();

            switch (frame.Type)
            {
                case Type.SETTINGS:
                    var s = frame as Settings;
                    var ackSettings = new Settings(0, Type.SETTINGS, SettingsFlags.Ack, s.StreamId, null);
                    await SendFrameAsync(ackSettings);
                    break;
                case Type.WINDOW_UPDATE:
                    var wu = frame as WindowUpdate;
                    break;
                case Type.HEADERS:
                    var h = frame as Headers;
                    break;
            }
            return true;
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
            var intValue = new byte[4];
            intValue[0] = header[2];
            intValue[1] = header[1];
            intValue[2] = header[0];
            var length = BitConverter.ToInt32(intValue);
            var type = (Http2.Type)header[3];
            var flags = header[4];
            intValue[0] = header[8];
            intValue[1] = header[7];
            intValue[2] = header[6];
            intValue[3] = header[5];
            var streamId = BitConverter.ToInt32(intValue);
            var payload = await ReadAsync(length);
            switch (type)
            {
                case Http2.Type.SETTINGS:
                    return new Settings(length, type, (SettingsFlags)flags, streamId, payload);
                case Http2.Type.WINDOW_UPDATE:
                    return new WindowUpdate(length, type, flags, streamId, payload);
                case Http2.Type.HEADERS:
                    return new Headers(length, type, (HeadersFlags)flags, streamId, payload);
                default:
                    throw new Exception("Frame type not supported");
            }
        }

        async Task SendFrameAsync(Frame frame)
        {
            var bytes = frame.Serialize();
            await networkStream.WriteAsync(bytes, 0, bytes.Length);
        }

        const string check = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n";
    }
}
