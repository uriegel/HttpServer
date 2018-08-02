using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

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
        public List<Isapi> Isapis { get; } = new List<Isapi>();
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
            var caesarFolder = new CaesarFolder(CaesarModule.WebServer);
            this.ConfigFolder = caesarFolder.ConfigurationPath;
            this.InstallFolder = caesarFolder.InstallPath;
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
                using (StreamReader sr = new StreamReader(file))
                {
                    string content = sr.ReadToEnd();
                    int start = content.IndexOf("CACHE:\r\n") + 8;
                    int stop = content.IndexOf("\r\n\r\n", start);
                    string caches = content.Substring(start, stop - start);
                    var cacheFiles = caches.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    ncf = ncf.Concat(cacheFiles.Select(n => Path.Combine(root, n.Replace('/', '\\')).ToLower()));
                }
            }
            return ncf.ToArray();
        }

        public ProxyRedirection CheckProxyRedirection(string urlBase)
        {
            return ProxyRedirections.FirstOrDefault(n => urlBase.StartsWith(n.RedirectionBaseUrl, StringComparison.CurrentCultureIgnoreCase));
        }

        public void EnableDefaultPHPSupport()
        {
            var ini = new IniSettings(Path.Combine(this.ConfigFolder, "cws.net.ini"));
            var php_ini = new IniSettings(Path.Combine(this.ConfigFolder, "php.ini"));

            FastCGIScripts.Add(new FastCGIScript(this, ".php",
                new FastCGIHandler(commandLine: $"\"{(Path.Combine(this.InstallFolder, @"php\php-cgi.exe"))}\" -c \"{(Path.Combine(this.ConfigFolder, @"php.ini"))}\"",
                                maxProcesses: ini.GetInt("FastCGI_php", "MaxProcesses", FastCGIHandler.DEF_MAX_PROCESSES),
                                maxParallelRequestsPerProcess: ini.GetInt("FastCGI_php", "MaxParallelRequestsPerProcess", FastCGIHandler.DEF_MAX_PARALLELREQUESTSPERPROCESS),
                                maxRequestsPerProcess: ini.GetInt("FastCGI_php", "MaxRequestsPerProcess", FastCGIHandler.DEF_MAX_REQUESTSPERPROCESS),
                                timeOut: ini.GetInt("FastCGI_php", "MaxExecutionTime", php_ini.GetInt("PHP", "max_execution_time", FastCGIHandler.DEF_TIMEOUT - 1) + 1),
                                configuration: this)));
        }

        #endregion
    }
}
