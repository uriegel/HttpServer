namespace WebServer

open System.Collections.Generic
open System.IO
open System.Net
open System.Security.Cryptography.X509Certificates
open System.Security.Authentication
open System.Threading
open System.Net.Sockets
open System
open Microsoft.Extensions.Logging

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
    member val TlsRedirect = false with get, set 
    member val Http2 = false with get, set 
    member val Certificate: X509Certificate2 = null with get, set 
    member val LowestProtocol = TlsProtocol.Tls10 with get, set 
    member val CheckRevocation = false with get, set 

    //member val  public string[] AppCaches { get; set; }

    member val CertificateName = null with get, set 
    member val HstsDurationInSeconds = 0 with get, set 
    member val XFrameOptions = XFrameOptions.NotSet with get, set 

    member internal this.TlsProtocols
        with get() =
            match this.LowestProtocol with
            | TlsProtocol.Tls11 -> SslProtocols.Tls11 ||| SslProtocols.Tls12
            | TlsProtocol.Tls12 -> SslProtocols.Tls12
            | _ -> SslProtocols.Tls ||| SslProtocols.Tls11 ||| SslProtocols.Tls12

module Server =
    let private log = Logger.log "Server"
    let getPort () = 
        match Configuration.Current.IsTlsEnabled with
        | true when Configuration.Current.TlsPort = 443 -> ""
        | true -> string Configuration.Current.TlsPort
        | false when Configuration.Current.Port = 80 -> ""
        | false -> string Configuration.Current.Port

    let getBaseUrl () =
        sprintf "http%s://%s%s" (if Configuration.Current.IsTlsEnabled then "s" else "") Configuration.Current.DomainName (getPort ())
    
    let mutable private isStarted = false
    let mutable private listener: TcpListener Option = None
    let mutable private tlsListener: TcpListener Option = None

    let private asyncOnConnected (tcpClient: TcpClient) isSecure =
        async {
            if isStarted then
                try
                    do! Processing.asyncStartReceiving tcpClient isSecure 
                with 
                | :? SocketException as se when se.NativeErrorCode = 10054 -> ()
                | :? ObjectDisposedException -> ()  // Stop() aufgerufen 
                | e when isStarted -> log LogLevel.Error (sprintf "Error in OnConnected occurred: %A" e)
        }
    let rec private asyncStartConnecting (listener: TcpListener) isSecure = 
        async {
            if isStarted then
                try 
                    let! client = listener.AcceptTcpClientAsync () |> Async.AwaitTask
                    asyncStartConnecting listener isSecure |> Async.StartImmediate
                    asyncOnConnected client isSecure |> Async.StartImmediate
                with 
                | :? SocketException as se when se.SocketErrorCode = SocketError.Interrupted && not isStarted -> ()
                | e -> log LogLevel.Error (sprintf "Error occurred in connecting thread: %A" e)
        }

    let createListener port = 
        let listener = Ipv6TcpListenerFactory.create port
        if not listener.Ipv6 then log LogLevel.Information "IPv6 or IPv6 dual mode not supported, switching to IPv4"
        listener.Listener

    let Start (configuration: InitializationData) = 
        if not isStarted then
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
                            | None -> None
                        | _ -> Some configuration.Certificate
                    | false -> None

                if configuration.HstsDurationInSeconds > 0 then
                    if configuration.IsTlsEnabled && configuration.TlsRedirect then
                        log LogLevel.Information (sprintf "Using HSTS: max-days=%A, max-age=%d" (configuration.HstsDurationInSeconds / (3600 * 24)) configuration.HstsDurationInSeconds)
                    else
                        log LogLevel.Warning "HSTS is only available when 'TlsEnabled=true' and 'TlsRedirect=true'"
                        configuration.HstsDurationInSeconds <- 0

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
                    TlsRedirect = configuration.TlsRedirect
                    Http2 = configuration.Http2
                    Certificate = 
                        if configuration.IsTlsEnabled then 
                            getCertificate () 
                        else 
                            None
                    CheckRevocation = configuration.CheckRevocation
                    //member val  public string[] AppCaches { get; set; }
                    //HstsDurationInSeconds = configuration.HstsDurationInSeconds
                    XFrameOptions = configuration.XFrameOptions
                    TlsProtocols = configuration.TlsProtocols
                }

                log LogLevel.Information "Starting Web Server"

                ServicePointManager.DefaultConnectionLimit <- 1000 
                ServicePointManager.SecurityProtocol <- SecurityProtocolType.Tls12 ||| SecurityProtocolType.Tls11 ||| SecurityProtocolType.Tls 
                ThreadPool.SetMinThreads (1000, 1000) |> ignore

                Configuration.Initialize (toSettings configuration)

                log LogLevel.Information (sprintf "Socket timeout: %ds" (Configuration.Current.SocketTimeout / 1000))
                log LogLevel.Information (sprintf "Domain name: %s" Configuration.Current.DomainName)

                if Configuration.Current.LocalAddress <> IPAddress.Any then
                    log LogLevel.Information (sprintf "Binding to local address: %s" (Configuration.Current.LocalAddress.ToString ()))
        
                let (listen, tlsListen) =
                    if Configuration.Current.IsTlsEnabled then
                        (configuration.TlsRedirect, true)
                    else
                        (true, false)

                if Configuration.Current.IsTlsEnabled then
                    log LogLevel.Information (sprintf "Supported secure protocols: %A" configuration.TlsProtocols)

                    match Configuration.Current.Certificate with
                    | Some certificate -> log LogLevel.Information (sprintf "Using certificate %A" certificate)
                    | None when (configuration.CertificateName <> null)
                         -> failwith (sprintf "No certificate with display name %A found" configuration.CertificateName)
                    | None -> failwith "No certificate specified"

                    if Configuration.Current.CheckRevocation then 
                        log LogLevel.Information "Checking revocation lists"

                log LogLevel.Information "Starting listener(s)..."
                isStarted <- true
                if listen then 
                    log LogLevel.Information (sprintf "Starting listener on port %d" configuration.Port)
                    listener <- Some (createListener configuration.Port)
                    listener.Value.Start ()
                    asyncStartConnecting listener.Value false |> Async.StartImmediate
                if tlsListen then 
                    log LogLevel.Information (sprintf "Starting secure listener on port %d" configuration.TlsPort)
                    tlsListener <- Some (createListener configuration.TlsPort)
                    tlsListener.Value.Start ()
                    asyncStartConnecting tlsListener.Value true |> Async.StartImmediate
                
                log LogLevel.Information "Listener(s) started"
                log LogLevel.Information "Web Server started"
            with 
            | :? SocketException as se when se.SocketErrorCode <> SocketError.AddressAlreadyInUse ->
                raise se
            | e ->
                log LogLevel.Warning (sprintf "Could not start HTTP Listener: %A" e)
                isStarted <- false
    let Stop () =
        if not isStarted then
            try
                log LogLevel.Information "Terminating managed extensions..."

                //let tasks = 
                //    Settings.Current.Extensions
                //    |> List.map (fun n -> n.
                //..Extensions .Select(n => n.ShutdownAsync());
                //Task.WhenAll(tasks.ToArray()).Synchronize();

                log LogLevel.Information "Managed extensions terminated"

                isStarted <- false

                match listener with
                    | Some value ->
                        log LogLevel.Information "Stopping listener..."
                        value.Stop ()
                        log LogLevel.Information "Listener stopped"
                    | None -> ()
                
                match tlsListener with
                    | Some value ->
                        log LogLevel.Information "Stopping HTTPS listener..."
                        value.Stop ()
                        log LogLevel.Information "HTTPS listener stopped"
                    | None -> ()
            with e -> log LogLevel.Warning (sprintf "Could not stop web server: %A" e)
