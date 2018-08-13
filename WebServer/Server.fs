namespace WebServer

open System.Collections.Generic
open System.IO
open System.Net
open System.Security.Cryptography.X509Certificates
open System.Security.Authentication
open System.Threading

type TlsProtocol =
    Tls10 = 0
    | Tls11 = 1
    | Tls12 = 2

type InitializationData() =
    member val LocalAddress = IPAddress.Any with get, set    
    member val Webroot = Directory.GetCurrentDirectory() with get, set 
    member val SocketTimeout = 20000 with get, set 
    member val Extensions = List<int>() with get
    member val DomainName = "" with get, set 
    //member val AllowOrigins = .Array<string>[0] { get; set; }
    member val Port = 80 with get, set 
    member val TlsPort = 443 with get, set 
    member val IsTlsEnabled = false with get, set 
    member val TlsTracing = false with get, set 
    member val TlsRedirect = false with get, set 
    member val Certificate: X509Certificate2 = null with get, set 
    member val LowestProtocol = TlsProtocol.Tls10 with get, set 
    member val CheckRevocation = false with get, set 

    //member val  public string[] AppCaches { get; set; }

    member val CertificateName = "URIEGEL" with get, set 
    member val HstsDurationInSeconds = 0 with get, set 
    member val XFrameOptions = XFrameOptions.NotSet with get, set 

    member internal this.TlsProtocols
        with get() =
            match this.LowestProtocol with
            | TlsProtocol.Tls11 -> SslProtocols.Tls11 ||| SslProtocols.Tls12
            | TlsProtocol.Tls12 -> SslProtocols.Tls12
            | _ -> SslProtocols.Tls ||| SslProtocols.Tls11 ||| SslProtocols.Tls12

module Server =
    let getPort () = 
        match Settings.Current.IsTlsEnabled with
        | true when Settings.Current.TlsPort = 443 -> ""
        | true -> string Settings.Current.TlsPort
        | false when Settings.Current.Port = 80 -> ""
        | false -> string Settings.Current.Port

    let getBaseUrl () =
        sprintf "http%s://%s%s" (if Settings.Current.IsTlsEnabled then "s" else "") Settings.Current.DomainName (getPort ())
    
    let Start (configuration: InitializationData) = 
        let toSettings (configuration: InitializationData) = {
            LocalAddress = configuration.LocalAddress
            Webroot = configuration.Webroot
            SocketTimeout = configuration.SocketTimeout
            Extensions = List.ofSeq configuration.Extensions
            DomainName = 
                match configuration.DomainName with
                | _ when System.String.IsNullOrEmpty configuration.DomainName -> (Dns.GetHostEntry (System.Environment.MachineName)).HostName
                | _ -> configuration.DomainName
            //member val AllowOrigins = .Array<string>[0] { get; set; }
            Port = if configuration.Port > 0 then configuration.Port else 80
            TlsPort = if configuration.TlsPort> 0 then configuration.TlsPort else 443
            IsTlsEnabled = configuration.IsTlsEnabled
            TlsTracing = configuration.TlsTracing
            TlsRedirect = configuration.TlsRedirect
            Certificate = configuration.Certificate
            CheckRevocation = configuration.CheckRevocation
            //member val  public string[] AppCaches { get; set; }
            CertificateName = configuration.CertificateName
            HstsDurationInSeconds = configuration.HstsDurationInSeconds
            XFrameOptions = configuration.XFrameOptions
            TlsProtocols = configuration.TlsProtocols
        }

        let initializeTls () = 
            use store = new X509Store (StoreLocation.LocalMachine)
            store.Open OpenFlags.ReadOnly
  //          if Settings.Current.Certificate = null then
//                let certificate = store.Certificates.Cast<X509Certificate2>().Where(n => n.FriendlyName == Configuration.CertificateName).FirstOrDefault();


            ()

        Logger.Info "Starting Web Server"

        ServicePointManager.DefaultConnectionLimit <- 1000 
        ServicePointManager.SecurityProtocol <- SecurityProtocolType.Tls12 ||| SecurityProtocolType.Tls11 ||| SecurityProtocolType.Tls 
        ThreadPool.SetMinThreads (1000, 1000) |> ignore

        Settings.Initialize (toSettings configuration)

        Logger.Info (sprintf "Socket timeout: %ds" (Settings.Current.SocketTimeout / 1000))
        Logger.Info (sprintf "Domain name: %s" Settings.Current.DomainName)

        if Settings.Current.LocalAddress <> IPAddress.Any then
            Logger.Info (sprintf "Binding to local address: %s" (Settings.Current.LocalAddress.ToString ()))
        if Settings.Current.IsTlsEnabled then
            Logger.Info("Initializing TLS")
            initializeTls ()
            Logger.Info (sprintf "Listening on secure port %d" Settings.Current.TlsPort)
                //var result = Ipv6TcpListenerFactory.Create(configuration.TlsPort);
                //tlsListener = result.Listener;
                //if (!result.Ipv6)
                //    Logger.Current.Info("IPv6 or IPv6 dual mode not supported, switching to IPv4");

                //if (configuration.TlsRedirect)
                //{
                //    Logger.Current.Info("Initializing TLS redirect");
                //    result = Ipv6TcpListenerFactory.Create(configuration.Port);
                //    tlsRedirectorListener = result.Listener;
                //    if (!result.Ipv6)
                //        Logger.Current.Info("IPv6 or IPv6 dual mode not supported, switching to IPv4");
                //}
                //Logger.Current.Info("TLS initialized");

            
        
        Logger.Info "Web Server started"

    let Stop () =
        ()
