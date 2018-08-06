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
                case FrameType.SETTINGS:
                    var settings = frame as Settings;
                    settings.Values?.TryGetValue(Settings.Identifier.SETTINGS_HEADER_TABLE_SIZE, out maxHeaderTableSize);
                    var ackSettings = Settings.CreateAck(settings.StreamId);
                    await SendFrameAsync(ackSettings);
                    break;
                case FrameType.WINDOW_UPDATE:
                    var wu = frame as WindowUpdate;
                    break;
                case FrameType.HEADERS:
                    hpackDecoder = new HPack.Decoder(frame as Headers, maxHeaderTableSize);
                    hpackDecoder.Decode();
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
            var type = (FrameType)header[3];
            var payload = await ReadAsync(length);
            switch (type)
            {
                case FrameType.SETTINGS:
                    return new Settings(header, payload);
                case FrameType.WINDOW_UPDATE:
                    return new WindowUpdate(header, payload);
                case FrameType.HEADERS:
                    return new Headers(header, payload);
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
        int maxHeaderTableSize;
        HPack.Decoder hpackDecoder;
    }
}
