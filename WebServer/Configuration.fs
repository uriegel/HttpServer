namespace WebServer
open System.Net
open System.Security.Authentication

module Configuration = 
    let defaultConfiguration = { 
        localAddress = IPAddress.Any
        webroot = ""
        socketTimeout = 20000
        domainName = (Dns.GetHostEntry (System.Environment.MachineName)).HostName
        //member val AllowOrigins = .Array<string>[0] { get; set; }
        port = 80
        tlsPort = 443
        isTlsEnabled = false
        tlsRedirect = false
        http2 = false
        certificate = None
        checkRevocation = false
        //member val  public string[] AppCaches { get; set; }
        // HstsDurationInSeconds = 0 // Not with LetsEncrypt
        xFrameOptions = XFrameOptions.NotSet
        tlsProtocols = SslProtocols.Tls ||| SslProtocols.Tls11 ||| SslProtocols.Tls12
        checkRequest = fun requestHeaders -> false
        request = fun requestHeaders -> async { () }
        sseCallback = None
    }

    let mutable private initialConfiguration = defaultConfiguration

    let setConfiguration configuration = 
        initialConfiguration <- configuration

    let current = lazy initialConfiguration
