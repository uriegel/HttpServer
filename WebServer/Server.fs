namespace WebServer

open System.Collections.Generic
open System.IO
open System.Net
open System.Security.Cryptography.X509Certificates
open System.Security.Authentication
open System.Threading
open System.Net.Sockets
open System

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
        try
            let getCertificate () = 
                match configuration.IsTlsEnabled with
                | true ->
                    match configuration.Certificate with
                    | null ->
                        use store = new X509Store (StoreLocation.LocalMachine)
                        store.Open OpenFlags.ReadOnly
                        let certificate = 
                            store.Certificates
                            |> Seq.cast<X509Certificate2>
                            |> Seq.filter (fun n -> n.FriendlyName = configuration.CertificateName)
                            |> Seq.tryItem 0
                        match certificate with
                        | Some value -> Some value
                        | None ->
                            let certificateFile = @"c:\users\urieg\desktop\Riegel.selfhost.eu.pfx"
                            //var certificateFile = @"d:\test\Riegel.selfhost.eu.pfx";
                            //var certificateFile = @"d:\test\zert.pfx";
                            //var certificateFile = @"d:\test\zertOhneAntragsteller.pfx";

                            //var certificateFile = @"D:\OpenSSL\bin\affe\key.pem";
                            let beits = Array.zeroCreate (int (FileInfo certificateFile).Length)
                            use file = File.OpenRead certificateFile
                            file.Read (beits, 0, beits.Length) |> ignore
                            Some (new X509Certificate2 (beits, "caesar"))
                            //var userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                            //Logger.Current.Info($"Searching in current user store: {userName}");
                            //store = new X509Store(StoreLocation.CurrentUser);
                            //store.Open(OpenFlags.ReadOnly);
                            //Configuration.Certificate = store.Certificates.Cast<X509Certificate2>().Where(n => n.FriendlyName == Configuration.CertificateName).FirstOrDefault();
                            //if (Configuration.Certificate != null)
                            //    Logger.Current.Info($"Using certificate from current user store: {userName}");
                    | _ -> Some configuration.Certificate
                | false -> None

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
                Certificate = getCertificate ()
                CheckRevocation = configuration.CheckRevocation
                //member val  public string[] AppCaches { get; set; }
                CertificateName = configuration.CertificateName
                HstsDurationInSeconds = configuration.HstsDurationInSeconds
                XFrameOptions = configuration.XFrameOptions
                TlsProtocols = configuration.TlsProtocols
            }

            Logger.Info "Starting Web Server"

            ServicePointManager.DefaultConnectionLimit <- 1000 
            ServicePointManager.SecurityProtocol <- SecurityProtocolType.Tls12 ||| SecurityProtocolType.Tls11 ||| SecurityProtocolType.Tls 
            ThreadPool.SetMinThreads (1000, 1000) |> ignore

            Settings.Initialize (toSettings configuration)

            Logger.Info (sprintf "Socket timeout: %ds" (Settings.Current.SocketTimeout / 1000))
            Logger.Info (sprintf "Domain name: %s" Settings.Current.DomainName)

            if Settings.Current.LocalAddress <> IPAddress.Any then
                Logger.Info (sprintf "Binding to local address: %s" (Settings.Current.LocalAddress.ToString ()))
        
            let (listener, tlsRedirectListener) =
                if Settings.Current.IsTlsEnabled then
                    Logger.Info (sprintf "Supported secure protocols: %A" configuration.TlsProtocols)

                    match Settings.Current.Certificate with
                    | Some certificate -> Logger.Info (sprintf "Using certificate %A" certificate)
                    | None -> failwith (sprintf "No certificate with display name %A found" Settings.Current.CertificateName)

                    if Settings.Current.CheckRevocation then 
                        Logger.Info ("Checking revocation lists")
            
                    Logger.Info (sprintf "Listening on secure port %d" Settings.Current.TlsPort)
                    let listener = Ipv6TcpListenerFactory.create configuration.TlsPort
                    if not listener.Ipv6 then
                        Logger.Info ("IPv6 or IPv6 dual mode not supported, switching to IPv4")

                    let tlsRedirectListener = 
                        if configuration.TlsRedirect then
                            Logger.Info("Initializing TLS redirect")
                            let listener = Ipv6TcpListenerFactory.create configuration.Port
                            if not listener.Ipv6 then 
                                Logger.Info ("IPv6 or IPv6 dual mode not supported, switching to IPv4")
                            Some listener
                        else
                            None

                    Logger.Info ("TLS initialized")
                    (listener, tlsRedirectListener)
                else
                    Logger.Info (sprintf "Listening on port %d" configuration.Port)
                    let listener = Ipv6TcpListenerFactory.create configuration.Port
                    if not listener.Ipv6 then 
                        Logger.Info ("IPv6 or IPv6 dual mode not supported, switching to IPv4")
                    (listener, None)

            Logger.Info ("Starting listener...")
            listener.Listener.Start ()
            Logger.Info ("Listener started")
        
            match tlsRedirectListener with
            | Some listener ->
                Logger.Info ("Starting HTTP redirection listener...")
                listener.Listener.Start ()
                Logger.Info ("HTTPS redirection listener started")
            | None -> ()
            
            Logger.Info "Web Server started"
            true
        with 
        | :? SocketException as se when se.SocketErrorCode <> SocketError.AddressAlreadyInUse ->
            raise se
        | e ->
            Logger.Warning (sprintf "Could not start HTTP Listener: %A" e)
            false

    let Stop () =
        ()
