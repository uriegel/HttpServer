using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using HttpServer.Enums;
using HttpServer.Isapi;

namespace HttpServer
{

    public class Configuration
    {
        #region Properties

        public string InstallFolder { get; }
        public IPAddress LocalAddress { get; set; } = IPAddress.Any;

        public string Webroot
        {
            get
            {
                if (string.IsNullOrEmpty(_Webroot))
                    _Webroot = Directory.GetCurrentDirectory();
                return _Webroot;
            }
            set { _Webroot = value; }
        }
        string _Webroot;

        public bool HeaderTracing
        {
            get
            {
                if (IsTlsEnabled)
                    return false;
                return true;
            }
        }

        public int SocketTimeout { get; set; } = 20000;
        public List<Extension> Extensions { get; } = new List<Extension>();
        public List<IsapiExtension> Isapis { get; } = new List<IsapiExtension>();
        public List<Redirection> Redirections { get; } = new List<Redirection>();
        public List<Alias> Aliases { get; } = new List<Alias>();
        public string DomainName { get; set; }
        public string[] AllowOrigins { get; set; }
        public int Port { get; set; } = 80;
        public int TlsPort { get; set; } = 443;
        public bool IsTlsEnabled { get; set; }
        public bool TlsTracing { get; set; }
        public bool TlsRedirect { get; set; }
        public X509Certificate2 Certificate { get; set; }
        public SslProtocols TlsProtocols { get; set; } = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
        public bool CheckRevocation { get; set; }

        public string[] AppCaches { get; set; }

        public string CertificateName { get; set; } = "CAESAR";
        public string[] NoCacheFiles
        {
            get
            {
                if (_NoCacheFiles == null)
                    _NoCacheFiles = InitializeAppCaches();
                return _NoCacheFiles;
            }
        }
        string[] _NoCacheFiles;

        public int HstsDurationInSeconds { get; set; }

        public XFrameOptions XFrameOptions { get; set; }

        #endregion

        #region Methods

        public Configuration()
        {
        }

        string[] InitializeAppCaches()
        {
            var ncf = Enumerable.Empty<string>();
            if (AppCaches == null)
                return new string[0];
            foreach (var appcache in AppCaches)
            {
                var file = Path.Combine(Webroot, appcache);
                var root = Path.GetDirectoryName(file);
                using (var sr = new StreamReader(file))
                {
                    var content = sr.ReadToEnd();
                    var start = content.IndexOf("CACHE:\r\n") + 8;
                    var stop = content.IndexOf("\r\n\r\n", start);
                    var caches = content.Substring(start, stop - start);
                    var cacheFiles = caches.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    ncf = ncf.Concat(cacheFiles.Select(n => Path.Combine(root, n.Replace('/', '\\')).ToLower()));
                }
            }
            return ncf.ToArray();
        }

        #endregion
    }
}
