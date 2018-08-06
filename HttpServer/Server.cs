using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HttpServer.Extensions;
using HttpServer.Interfaces;
using HttpServer.Sessions;

namespace HttpServer
{
    public class Server : IServer
    {
        #region Properties

        public int Version { get { return 2; } }

        public string PhysicalPath { get { return Configuration.Webroot; } }

        public Assembly Assembly { get { return Assembly.GetExecutingAssembly(); } }

        public Configuration Configuration { get; private set; }

        public bool IsStarted { get; private set; }

        public string BaseUrl { get; }

        #endregion

        #region Constructor

        public Server(Configuration configuration)
        {
            Logger.Current.Info("Initializing Server...");
            Configuration = configuration;
            Logger.Current.Info($"Socket timeout: {Configuration.SocketTimeout / 1000}s");
            if (string.IsNullOrEmpty(configuration.DomainName))
                configuration.DomainName = Dns.GetHostEntry(Environment.MachineName).HostName;
            Logger.Current.Info($"Domain name: {configuration.DomainName}");

            string GetPort()
            {
                if ((configuration.IsTlsEnabled && configuration.TlsPort == 443) || (!configuration.IsTlsEnabled && configuration.Port == 80))
                    return "";

                return $":{(configuration.IsTlsEnabled ? configuration.TlsPort : configuration.Port)}";
            }

            BaseUrl = $"http{(configuration.IsTlsEnabled ? "s" : "")}://{configuration.DomainName}{GetPort()}";

            if (configuration.LocalAddress != IPAddress.Any)
                Logger.Current.Info($"Binding to local address: {configuration.LocalAddress}");

            if (Configuration.IsTlsEnabled)
            {
                Logger.Current.Info("Initializing TLS");

                InitializeTls();
                Logger.Current.Info($"Listening on secure port {configuration.TlsPort}");
                var result = Ipv6TcpListenerFactory.Create(configuration.TlsPort);
                tlsListener = result.Listener;
                if (!result.Ipv6)
                    Logger.Current.Info("IPv6 or IPv6 dual mode not supported, switching to IPv4");

                if (configuration.TlsRedirect)
                {
                    Logger.Current.Info("Initializing TLS redirect");
                    result = Ipv6TcpListenerFactory.Create(configuration.Port);
                    tlsRedirectorListener = result.Listener;
                    if (!result.Ipv6)
                        Logger.Current.Info("IPv6 or IPv6 dual mode not supported, switching to IPv4");
                }
                Logger.Current.Info("TLS initialized");
            }
            else if (!Configuration.TlsRedirect)
            {
                Logger.Current.Info($"Listening on port {configuration.Port}");
                var result = Ipv6TcpListenerFactory.Create(configuration.Port);
                listener = result.Listener;
                if (!result.Ipv6)
                    Logger.Current.Info("IPv6 or IPv6 dual mode not supported, switching to IPv4");
            }

            Logger.Current.Info("Server initialized");
        }

        static Server()
        {
            // TODO:
            ServicePointManager.DefaultConnectionLimit = 1000;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ThreadPool.SetMinThreads(1000, 1000);
        }

        #endregion

        #region Methods

        public void Start()
        {
            try
            {
                Logger.Current.Info("Starting Web Server");
                Resources.Current.Initialize();
                Initialize();

                if (listener != null)
                {
                    Logger.Current.Info("Starting HTTP listener...");
                    listener.Start();
                    Logger.Current.Info("HTTP listener started");
                }
                if (tlsListener != null)
                {
                    Logger.Current.Info("Starting HTTPS listener...");
                    tlsListener.Start();
                    Logger.Current.Info("HTTPS listener started");
                }

                IsStarted = true;
                if (listener != null)
                    StartConnecting(listener, false);
                if (tlsListener != null)
                    StartConnecting(tlsListener, true);

                if (tlsRedirectorListener != null)
                {
                    Logger.Current.Info("Starting HTTP redirection listener...");
                    tlsRedirectorListener.Start();
                    StartTlsRedirecting();
                    Logger.Current.Info("HTTPS redirection listener started");
                }

                if (Configuration.HstsDurationInSeconds > 0)
                {
                    if (Configuration.IsTlsEnabled && Configuration.TlsRedirect)
                        Logger.Current.Info($"Using HSTS: max-days={Configuration.HstsDurationInSeconds / (3600 * 24)}, max-age={Configuration.HstsDurationInSeconds}");
                    else
                    {
                        Logger.Current.Warning($"HSTS is only available when 'TlsEnabled=true' and 'TlsRedirect=true'");
                        Configuration.HstsDurationInSeconds = 0;
                    }
                }

                Logger.Current.Info("Web Server started");
            }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                throw;
            }
            catch (Exception e)
            {
                IsStarted = false;
                Logger.Current.Warning($"Could not start HTTP Listener: {e}");
            }
        }

        public void Stop()
        {
            try
            {
                Logger.Current.Info("Terminating managed extensions...");

                var tasks = Configuration.Extensions.Select(n => n.ShutdownAsync());
                Task.WhenAll(tasks.ToArray()).Synchronize();

                Logger.Current.Info("Managed extensions terminated");

                IsStarted = false;

                if (listener != null)
                {
                    Logger.Current.Info("Stopping HTTP listener...");
                    listener.Stop();
                    Logger.Current.Info("HTTP listener stopped");
                }

                if (tlsListener != null)
                {
                    Logger.Current.Info("Stopping HTTPS listener...");
                    tlsListener.Stop();
                    Logger.Current.Info("HTTPS listener stopped");
                }
                if (tlsRedirectorListener != null)
                {
                    Logger.Current.Info("Stopping HTTPS redirection listener...");
                    tlsRedirectorListener.Stop();
                    Logger.Current.Info("HTTPS redirection listener stopped");
                }

                Logger.Current.Info("Terminating isapis...");
                var isapis = Configuration.Isapis.ToArray();
                Configuration.Isapis.Clear();
                foreach (var isapi in isapis)
                    isapi.Terminate();
                Logger.Current.Info("isapis terminated");
            }
            catch (Exception e)
            {
                Logger.Current.Warning($"Could not stop web server: {e}");
            }
        }

        public void SetAlias(string value, string path) => Configuration.Aliases.Add(new Alias(value, path, null));

        public void RegisterCounter(string name, Func<int[]> getCounter)
            => Resources.Current.RegisterCounter(name, getCounter);

        public void RegisterAsyncFormHandler(string path, IFormHandler formHandler)
            => Extension.FormHandlers[path] = formHandler;

        public void InitializeExtensions()
        {
            ThreadPool.QueueUserWorkItem(async s =>
            {
                Logger.Current.Info($"Initializing extensions");

                var tasks = Configuration.Extensions.Select(n => n.InitializeAsync(this)).ToArray();
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch { }
            });
        }

        void Initialize()
        {
            try
            {
                Logger.Current.Info($"Supported secure protocols: {Configuration.TlsProtocols}");
            }
            catch (Exception e)
            {
                Logger.Current.Warning($"An error has occurred while initializing: {e}");
            }
        }

        void InitializeTls()
        {
            var store = new X509Store(StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            if (Configuration.Certificate == null)
            {
                Configuration.Certificate = store.Certificates.Cast<X509Certificate2>().Where(n => n.FriendlyName == Configuration.CertificateName).FirstOrDefault();
                if (Configuration.Certificate == null)
                {
                    var certificateFile = @"c:\users\urieg\desktop\Riegel.selfhost.eu.pfx";
                    //var certificateFile = @"d:\test\Riegel.selfhost.eu.pfx";
                    //var certificateFile = @"d:\test\zert.pfx";
                    //var certificateFile = @"d:\test\zertOhneAntragsteller.pfx";

                    //var certificateFile = @"D:\OpenSSL\bin\affe\key.pem";
                    var beits = new byte[new FileInfo(certificateFile).Length];
                    using (var file = File.OpenRead(certificateFile))
                        file.Read(beits, 0, beits.Length);
                    Configuration.Certificate = new X509Certificate2(beits, "caesar");
                    //var userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                    //Logger.Current.Info($"Searching in current user store: {userName}");
                    //store = new X509Store(StoreLocation.CurrentUser);
                    //store.Open(OpenFlags.ReadOnly);
                    //Configuration.Certificate = store.Certificates.Cast<X509Certificate2>().Where(n => n.FriendlyName == Configuration.CertificateName).FirstOrDefault();
                    //if (Configuration.Certificate != null)
                    //    Logger.Current.Info($"Using certificate from current user store: {userName}");
                }
                if (Configuration.Certificate != null)
                    Logger.Current.Info($"Using certificate {Configuration.Certificate}");
                else
                    Logger.Current.Fatal($@"No certificate with display name ""{Configuration.CertificateName}"" found");
            }
            if (Configuration.CheckRevocation)
                Logger.Current.Info("Checking revocation lists");
        }

        void StartConnecting(TcpListener listener, bool isSecured)
        {
            if (!IsStarted)
                return;

            new Thread(() =>
            {
                try
                {
                    while (IsStarted)
                    {
                        var client = listener.AcceptTcpClient();
                        OnConnected(client, isSecured);
                    }
                }
                catch (SocketException se) when (se.SocketErrorCode == SocketError.Interrupted && !IsStarted)
                {
                }
                catch (Exception e)
                {
                    Logger.Current.Fatal($"Error occurred in connecting thread: {e}");
                }
            })
            {
                IsBackground = true
            }.Start();
        }

        void StartTlsRedirecting()
        {
            new Thread(() =>
            {
                try
                {
                    while (IsStarted)
                    {
                        var client = tlsRedirectorListener.AcceptTcpClient();
                        var redirectSession = new TlsRedirectSession(this, client);
                        redirectSession.TlsRedirect();
                    }
                }
                catch (SocketException se) when (se.SocketErrorCode == SocketError.Interrupted && !IsStarted)
                {
                }
                catch (Exception e)
                {
                    Logger.Current.Fatal($"Error in StartTlsRedirecting occurred: {e}");
                }
            })
            {
                IsBackground = true
            }.Start();
        }

        void OnConnected(TcpClient tcpClient, bool isSecured)
        {
            try
            {
                if (!IsStarted)
                    return;
                SocketSession.StartReceiving(this, tcpClient, isSecured);
            }
            catch (SocketException se) when (se.NativeErrorCode == 10054)
            { }
            catch (ObjectDisposedException)
            {
                // Stop() aufgerufen
                return;
            }
            catch (Exception e)
            {
                if (!IsStarted)
                    return;
                Logger.Current.Fatal($"Error in OnConnected occurred: {e}");
            }
        }

        #endregion

        #region Fields

        readonly TcpListener listener;
        readonly TcpListener tlsListener;
        readonly TcpListener tlsRedirectorListener;

        #endregion
    }
}
