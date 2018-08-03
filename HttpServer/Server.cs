using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HttpServer.Interfaces;

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

        void InitializeTls()
        {
            var store = new X509Store(StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            if (Configuration.Certificate == null)
            {
                Configuration.Certificate = store.Certificates.Cast<X509Certificate2>().Where(n => n.FriendlyName == Configuration.CertificateName).FirstOrDefault();
                if (Configuration.Certificate == null)
                {
                    var userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                    Logger.Current.Info($"Searching in current user store: {userName}");
                    store = new X509Store(StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadOnly);
                    Configuration.Certificate = store.Certificates.Cast<X509Certificate2>().Where(n => n.FriendlyName == Configuration.CertificateName).FirstOrDefault();
                    if (Configuration.Certificate != null)
                        Logger.Current.Info($"Using certificate from current user store: {userName}");
                }
                if (Configuration.Certificate != null)
                    Logger.Current.Info($"Using certificate {Configuration.Certificate}");
                else
                    Logger.Current.Fatal($@"No certificate with display name ""{Configuration.CertificateName}"" found");
            }
            if (Configuration.CheckRevocation)
                Logger.Current.Info("Checking revocation lists");
        }

        #endregion

        #region Fields

        readonly TcpListener listener;
        readonly TcpListener tlsListener;
        readonly TcpListener tlsRedirectorListener;

        #endregion
    }
}
