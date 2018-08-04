using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HttpServer.Enums;
using HttpServer.Exceptions;
using HttpServer.Interfaces;
using HttpServer.Sessions;

namespace HttpServer.WebSockets
{
    class WebSocketSession : IWebSocketInternalSession
    {
        public static Counter Instances { get; } = new Counter();

        public string[] Protocols { get => protocols; }
        readonly string[] protocols;

        public int Id { get => id; }
        readonly int id;

        public bool IsSecureConnection { get => session?.IsSecureConnection ?? false; }
        public IPEndPoint LocalEndPoint { get => session?.LocalEndPoint; }
        public IPEndPoint RemoteEndPoint { get => session?.RemoteEndPoint; }
        public string UserAgent { get => session?.Headers?.UserAgent; }

        public WebSocketSession(RequestSession session, IServer server, Configuration configuration)
        {
            Instances.Increment();
            id = Interlocked.Increment(ref idSeed);
            this.session = session;
            this.server = server;
            this.configuration = configuration;
            host = session.Headers.Host;
            protocols = session.Headers["sec-websocket-protocol"]?.Split(new[] { ',' }) ?? new string[0];
            networkStream = session.GetNetworkStream();
        }

        public void Initialize(Func<string, Task> onMessage, Func<Task> onClosed)
        {
            this.onMessage = onMessage;
            this.onClosed = onClosed;
        }

        public Task SendAsync(string payload)
        {
            var memStm = new MemoryStream(Encoding.UTF8.GetBytes(payload));
            return WriteStreamAsync(memStm);
        }

        public Task SendJsonAsync(object jsonObject)
        {
            var type = jsonObject.GetType();
            var jason = new DataContractJsonSerializer(type);
            var memStm = new MemoryStream();
            jason.WriteObject(memStm, jsonObject);
            memStm.Position = 0;
            return WriteStreamAsync(memStm);
        }

        public void StartMessageReceiving()
        {
            var wsr = new WebSocketReceiver(networkStream);
            wsr.BeginMessageReceiving(async (wsDecodedStream, exception) =>
            {
                try
                {
                    if (isClosed || exception != null)
                    {
                        isClosed = true;
                        Logger.Current.LowTrace(() => "Connection closed");
                        if (exception is ConnectionClosedException)
                            await OnClose();
                        Instances.DecrementActive();
                    }
                    else
                    {
                        var payload = wsDecodedStream.Payload;
                        await OnMessage(payload);
                    }
                }
                catch (Exception e)
                {
                    Logger.Current.Warning($"Exception occurred while processing web socket request: {e}");
                    Instances.DecrementActive();
                }
            }, this);
        }

        public void Close()
        {
            if (isClosed)
                return;
            Instances.DecrementActive();
            isClosed = true;
            try
            {
                networkStream.Close();
            }
            catch { }
        }

        public async Task SendPongAsync(string payload)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
            await WriteStreamAsync(stream, OpCode.Pong);
        }

        async Task WriteStreamAsync(Stream payloadStream, OpCode? opCode = null)
        {
            try
            {
                Serialize(payloadStream, out var buffer, out var position, opCode);

                await semaphoreSlim.WaitAsync();
                try
                {
                    var read = 0;
                    while (read < payloadStream.Length)
                    {
                        var newlyRead = payloadStream.Read(buffer, position, buffer.Length - position);
                        await networkStream.WriteAsync(buffer, 0, newlyRead + position);
                        position = 0;
                        read += newlyRead;
                    }
                }
                catch
                {
                    try
                    {
                        networkStream.Close();
                    }
                    catch { }
                    throw new ConnectionClosedException();
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            }
            catch (ConnectionClosedException)
            {
                try
                {
                    OnClose().Wait();
                }
                catch { }
            }
        }

        static void Serialize(Stream payloadStream, out byte[] buffer, out int position, OpCode? opcode = null)
        {
            if (opcode == null)
                opcode = OpCode.Text;
            var length = (int)payloadStream.Length;
            var FRRROPCODE = (byte)(0x80 + (byte)(int)opcode.Value); //'FIN is set, and OPCODE is 1 (Text) or opCode

            int headerLength;
            if (length <= 125)
                headerLength = 2;
            else if (length <= ushort.MaxValue)
                headerLength = 4;
            else
                headerLength = 10;
            buffer = new byte[Math.Min(20000, headerLength + length)];
            position = 0;
            if (length <= 125)
            {
                buffer[0] = FRRROPCODE;
                buffer[1] = Convert.ToByte(length);
                position = 2;
            }
            else if (length <= ushort.MaxValue)
            {
                buffer[0] = FRRROPCODE;
                buffer[1] = 126;
                var sl = (ushort)length;
                var byteArray = BitConverter.GetBytes(sl);
                var eins = byteArray[0];
                buffer[2] = byteArray[1];
                buffer[3] = eins;
                position = 4;
            }
            else
            {
                buffer[0] = FRRROPCODE;
                buffer[1] = 127;
                var byteArray = BitConverter.GetBytes((ulong)length);
                var eins = byteArray[0];
                var zwei = byteArray[1];
                var drei = byteArray[2];
                var vier = byteArray[3];
                var fünf = byteArray[4];
                var sechs = byteArray[5];
                var sieben = byteArray[6];
                buffer[2] = byteArray[7];
                buffer[3] = sieben;
                buffer[4] = sechs;
                buffer[5] = fünf;
                buffer[6] = vier;
                buffer[7] = drei;
                buffer[8] = zwei;
                buffer[9] = eins;
                position = 10;
            }
        }

        Task OnMessage(string payload) => onMessage(payload);

        async Task OnClose() => await onClosed();

        static int idSeed;
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        RequestSession session;
        IServer server;
        Configuration configuration;
        Stream networkStream;
        string host;
        object locker = new object();
        bool isClosed;
        Func<string, Task> onMessage;
        Func<Task> onClosed;
    }
}
