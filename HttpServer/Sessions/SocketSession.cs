using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HttpServer.Exceptions;

namespace HttpServer.Sessions
{
    // TODO: IDisposable und using
    /// <summary>
    /// Bei HTTP wird die Socket für mehrere Aufrufe wiederverwendet.
    /// Hiermit wird eine solche Session implementiert, im Gegensatz zur logischen <see cref="RequestSession"/>, die bei jedem Aufruf neu angelegt wird
    /// </summary>
    class SocketSession : IDisposable
    {
        #region Properties	

        public static Counter Instances { get; } = new Counter();

        public int Id { get; private set; }

        public TcpClient Client { get; private set; }

        public bool UseTls { get; }

        #endregion

        #region Constructor	

        public SocketSession(Server server, TcpClient client, bool useTls)
        {
            this.UseTls = useTls;
            Id = Interlocked.Increment(ref idSeed);
            IncrementInstance();
            Logger.Current.LowTrace(() => $"{Id}- New {(useTls ? "secure " : "")}socket session created: - {(client.Client.RemoteEndPoint as IPEndPoint)}");
            this.server = server;
            this.Client = client;
            client.ReceiveTimeout = server.Configuration.SocketTimeout;
            client.SendTimeout = server.Configuration.SocketTimeout;
        }

        #endregion

        #region Methods	

        public static async void StartReceiving(Server server, TcpClient tcpClient, bool isSecured)
        {
            using (var session = new SocketSession(server, tcpClient, isSecured))
                await session.ReceiveAsync();
        }

        public async Task ReceiveAsync()
        {
            string id = null;
            try
            {
                while (true)
                {
                    if (networkStream == null)
                        networkStream = UseTls ? GetTlsNetworkStream(Client) : Client.GetStream();

                    using (var session = new RequestSession(server, this, networkStream))
                    {
                        id = session.Id;
                        if (!await session.StartAsync())
                            break;
                    }
                }
            }
            catch (AuthenticationException ae)
            {
                Logger.Current.Warning($"{Id}- An authentication error has occurred while reading socket, session: {Client.Client.RemoteEndPoint as IPEndPoint}, error: {ae}");
            }
            catch (Exception e) when (e is IOException || e is ConnectionClosedException || e is SocketException)
            {
                Logger.Current.LowTrace(() => $"{Id}- Closing socket session, reason: {e}");
                Close();
            }
            catch (Exception e) when (e is ObjectDisposedException)
            {
                Logger.Current.Trace($"{Id}- Object disposed");
                Close();
            }
            catch (Exception e)
            {
                Logger.Current.Warning($"{Id}- An error has occurred while reading socket, error: {e}");
            }
        }

        public void Close() => Client.Close();

        protected virtual void IncrementInstance() => Instances.Increment();

        protected virtual void DecrementInstance() => Instances.Decrement();

        static Stream GetTlsNetworkStream(NetworkStream stream, X509Certificate2 certificate)
        {
            var sslStream = new SslStream(stream);
            // TODO: einstellbar
            sslStream.AuthenticateAsServer(certificate, false, System.Security.Authentication.SslProtocols.Tls | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls12, true);
            var welches = sslStream.SslProtocol;
            return sslStream;
        }

        Stream GetTlsNetworkStream(TcpClient tcpClient)
        {
            var stream = tcpClient.GetStream();
            if (!server.Configuration.IsTlsEnabled)
                return null;

            var sslStream = new SslStream(stream);
            sslStream.AuthenticateAsServer(server.Configuration.Certificate, false, server.Configuration.TlsProtocols, server.Configuration.CheckRevocation);

            Logger.Current.LowTrace(() =>
            {
                Func<SslStream, string> getKeyExchangeAlgorithm = n => (int)n.KeyExchangeAlgorithm == 44550 ? "ECDHE" : $"{n.KeyExchangeAlgorithm}";
                Func<SslStream, string> getHashAlgorithm = n =>
                {
                    switch ((int)n.HashAlgorithm)
                    {
                        case 32781:
                            return "SHA384";
                        case 32780:
                            return "SHA256";
                        default:
                            return $"{n.HashAlgorithm}";
                    }
                };
                return $"{Id}- Secure protocol: {sslStream.SslProtocol}, Cipher: {sslStream.CipherAlgorithm} strength {sslStream.CipherStrength}, Key exchange: {getKeyExchangeAlgorithm(sslStream)} strength {sslStream.KeyExchangeStrength}, Hash: {getHashAlgorithm(sslStream)} strength {sslStream.HashStrength}";
            });

            return sslStream;
        }

        #endregion

        #region Fields	

        static int idSeed;
        protected Server server;
        protected Stream networkStream;

        #endregion

        #region IDisposable Support

        bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    DecrementInstance();

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}
