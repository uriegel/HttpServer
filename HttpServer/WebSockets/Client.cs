using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HttpServer.Exceptions;

namespace HttpServer.WebSockets
{
    class Client
    {
        public Client(string url, Func<string, Task> onMessage, Action onClosed)
        {
            this.url = new UrlExtracts(url);
            this.onMessage = onMessage;
            this.onClosed = onClosed;
        }

        static Client() => ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        public async Task OpenAsync()
        {
            Reset();

            synchronizationContext = SynchronizationContext.Current;

            await tcpClient.ConnectAsync(url.Host, url.Port);
            networkStream = tcpClient.GetStream();

            if (url.TlsUsed)
            {
                var secureStream = new SslStream(networkStream);
                await secureStream.AuthenticateAsClientAsync(url.Host);
                networkStream = secureStream;
            }

            await InitializeWebSockets();
            StartMessageReceiving();
        }

        public void Close()
        {
            try
            {
                var end = new byte[2];
                end[0] = 0xf8;
                networkStream.Write(end, 0, end.Length);
            }
            catch
            {
                try
                {
                    networkStream.Close();
                }
                catch { }
            }
        }

        public async Task SendAsync(string payload)
        {
            if (networkStream == null)
                throw new ConnectionClosedException();
            var bytes = Encoding.UTF8.GetBytes(payload);
            bytes = EncodeWSBuffer(bytes);
            await networkStream.WriteAsync(bytes, 0, bytes.Length);
        }

        public async Task SendJsonAsync(object jsonObject)
        {
            if (networkStream == null)
                throw new ConnectionClosedException();

            var type = jsonObject.GetType();
            var jason = new DataContractJsonSerializer(type);
            var memStm = new MemoryStream();

            jason.WriteObject(memStm, jsonObject);
            memStm.Position = 0;
            var bytes = memStm.ToArray();
            bytes = EncodeWSBuffer(bytes);

            await networkStream.WriteAsync(bytes, 0, bytes.Length);
        }

        public override string ToString() => $"Url: {url}, connected: {tcpClient?.Connected ?? false}";

        async Task InitializeWebSockets()
        {
            var key = DateTime.Now.ToString();
            var base64Key = Convert.ToBase64String(Encoding.UTF8.GetBytes(key));
            var http =
$@"GET {url.Url} HTTP/1.1
Upgrade: websocket
Connection: Upgrade
Host: {url.Host}
Sec-WebSocket-Key: {base64Key}

";
            var bytes = Encoding.UTF8.GetBytes(http);
            await networkStream.WriteAsync(bytes, 0, bytes.Length);

            var buffer = new byte[2000];

            async Task<bool> readAnswer(int pos)
            {
                var read = await networkStream.ReadAsync(buffer, pos, buffer.Length - pos);
                var result = read == 0;
                if (!result)
                {
                    var test = Encoding.UTF8.GetString(buffer, 0, pos + read);
                    result = test.Contains("\r\n\r\n");
                }
                return result ? result : await readAnswer(pos + read);
            }

            await readAnswer(0);
        }

        void StartMessageReceiving()
        {
            var wsr = new WebSocketReceiver(networkStream);
#pragma warning disable 1998
            wsr.BeginMessageReceiving(async (wsDecodedStream, exception) =>
            {
                try
                {
                    if (exception == null)
                    {
                        var payload = wsDecodedStream.Payload;
                        if (synchronizationContext != null)
                            synchronizationContext.Send(_ => onMessage(payload), null);
                        else
                            await onMessage(payload);
                    }
                    else
                    {
                        Close();
                        if (synchronizationContext != null)
                            synchronizationContext.Send(_ => onClosed(), null);
                        else
                            onClosed();
                    }
                }
                catch { }
            }, null);
        }

        byte[] EncodeWSBuffer(byte[] bytes)
        {
            byte[] result = null;
            var key = new byte[] { 9, 8, 6, 5 };

            var bufferIndex = 0;
            if (bytes.Length < 126)
            {
                result = new byte[bytes.Length + 6];
                result[1] = (byte)(bytes.Length + 128); // wahrscheinlich mit bit 8 gesetzt, Masking

                result[2] = key[0]; // Maskierung des Clients
                result[3] = key[1]; // Maskierung des Clients
                result[4] = key[2]; // Maskierung des Clients
                result[5] = key[3]; // Maskierung des Clients
                bufferIndex = 6;
            }
            else if (bytes.Length <= ushort.MaxValue)
            {
                result = new byte[bytes.Length + 8];
                result[1] = (byte)(126 + 128); // wahrscheinlich mit bit 8 gesetzt, Masking
                var sl = (ushort)bytes.Length;
                var byteArray = BitConverter.GetBytes(sl);
                var eins = byteArray[0];
                result[2] = byteArray[1];
                result[3] = eins;

                result[4] = key[0]; // Maskierung des Clients
                result[5] = key[1]; // Maskierung des Clients
                result[6] = key[2]; // Maskierung des Clients
                result[7] = key[3]; // Maskierung des Clients
                bufferIndex = 8;
            }
            result[0] = 129; // Text, single frame

            for (var i = 0; i < bytes.Length; i++)
                result[i + bufferIndex] = (Byte)(bytes[i] ^ key[i % 4]);

            return result;
        }

        void Reset()
        {
            try
            {
                var stream = networkStream;
                stream?.Dispose();
                var zombie = tcpClient;
                zombie?.Close();
            }
            catch { }

            networkStream = null;
            tcpClient = new TcpClient();
        }

        readonly UrlExtracts url;
        TcpClient tcpClient;
        SynchronizationContext synchronizationContext;
        Stream networkStream;
        Func<string, Task> onMessage;
        readonly Action onClosed;
    }
}
