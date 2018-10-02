namespace WebServer
open System.Net
open System.Security.Authentication

module Configuration = 
    let defaultConfiguration = { 
        LocalAddress = IPAddress.Any
        Webroot = ""
        SocketTimeout = 20000
        //Extensions = []
        DomainName = (Dns.GetHostEntry (System.Environment.MachineName)).HostName
        //member val AllowOrigins = .Array<string>[0] { get; set; }
        Port = 80
        TlsPort = 443
        IsTlsEnabled = false
        TlsRedirect = false
        Http2 = false
        Certificate = None
        CheckRevocation = false
        //member val  public string[] AppCaches { get; set; }
        // HstsDurationInSeconds = 0 // Not with LetsEncrypt
        XFrameOptions = XFrameOptions.NotSet
        TlsProtocols = SslProtocols.Tls ||| SslProtocols.Tls11 ||| SslProtocols.Tls12
    }

    let mutable private initialConfiguration = defaultConfiguration

    let setConfiguration configuration = 
        initialConfiguration <- configuration

    let current = lazy initialConfiguration
