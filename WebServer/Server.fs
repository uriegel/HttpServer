namespace WebServer
open System
open System.Diagnostics
open System.Net
open System.Security.Cryptography.X509Certificates
open System.Threading
open System.Net.Sockets
open Microsoft.Extensions.Logging

type TlsProtocol =
    Tls10 = 0
    | Tls11 = 1
    | Tls12 = 2

module Server =

    let private log = Logger.log "Server"
    let getPort () = 
        let configuration = Configuration.current.Force ()

        match configuration.isTlsEnabled with
        | true when configuration.tlsPort = 443 -> ""
        | true -> string configuration.tlsPort
        | false when configuration.port = 80 -> ""
        | false -> string configuration.port

    let getBaseUrl () =
        let configuration = Configuration.current.Force ()
        sprintf "http%s://%s%s" (if configuration.isTlsEnabled then "s" else "") configuration.domainName (getPort ())
    
    let mutable private isStarted = false
    let mutable private listener: TcpListener Option = None
    let mutable private tlsListener: TcpListener Option = None

    let private asyncOnConnected (tcpClient: TcpClient) isSecure stopwatch =
        async {
            if isStarted then
                try
                    do! Processing.asyncStartReceiving tcpClient isSecure stopwatch
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
                    let stopwatch = Stopwatch ()
                    stopwatch.Start ()
                    asyncStartConnecting listener isSecure |> Async.StartImmediate
                    asyncOnConnected client isSecure stopwatch |> Async.StartImmediate
                with 
                | :? SocketException as se when se.SocketErrorCode = SocketError.Interrupted && not isStarted -> ()
                | e -> log LogLevel.Error (sprintf "Error occurred in connecting thread: %A" e)
        }

    let createListener port = 
        let listener = Ipv6TcpListenerFactory.create port
        if not listener.ipv6 then log LogLevel.Information "IPv6 or IPv6 dual mode not supported, switching to IPv4"
        listener.listener

    let getCertificate name = 
        use store = new X509Store (StoreLocation.LocalMachine)
        store.Open OpenFlags.ReadOnly
        store.Certificates
        |> Seq.cast<X509Certificate2>
        |> Seq.filter (fun n -> n.FriendlyName = name)
        |> Seq.tryItem 0

    let start configuration = 
        if not isStarted then
            try
                Configuration.setConfiguration configuration
                log LogLevel.Information "Starting Web Server"

                ServicePointManager.DefaultConnectionLimit <- 1000 
                ServicePointManager.SecurityProtocol <- SecurityProtocolType.Tls12 ||| SecurityProtocolType.Tls11 ||| SecurityProtocolType.Tls 
                ThreadPool.SetMinThreads (1000, 1000) |> ignore

                log LogLevel.Information (sprintf "Socket timeout: %ds" (configuration.socketTimeout / 1000))
                log LogLevel.Information (sprintf "Domain name: %s" configuration.domainName)

                log LogLevel.Information (sprintf "Web root: %s" configuration.webroot) 

                if configuration.localAddress <> IPAddress.Any then
                    log LogLevel.Information (sprintf "Binding to local address: %s" (configuration.localAddress.ToString ()))
        
                let (listen, tlsListen) =
                    if configuration.isTlsEnabled then
                        (configuration.tlsRedirect, true)
                    else
                        (true, false)

                if configuration.isTlsEnabled then
                    log LogLevel.Information (sprintf "Supported secure protocols: %A" configuration.tlsProtocols)

                    match configuration.certificate with
                    | Some certificate -> log LogLevel.Information (sprintf "Using certificate %A" certificate)
                    | None -> failwith "No certificate specified"

                    if configuration.checkRevocation then 
                        log LogLevel.Information "Checking revocation lists"

                log LogLevel.Information "Starting listener(s)..."
                isStarted <- true
                if listen then 
                    log LogLevel.Information (sprintf "Starting listener on port %d" configuration.port)
                    listener <- Some (createListener configuration.port)
                    listener.Value.Start ()
                    asyncStartConnecting listener.Value false |> Async.StartImmediate
                if tlsListen then 
                    log LogLevel.Information (sprintf "Starting secure listener on port %d" configuration.tlsPort)
                    tlsListener <- Some (createListener configuration.tlsPort)
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
    let stop () =
        if isStarted then
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
                log LogLevel.Information "Web Server stopped"
            with e -> log LogLevel.Warning (sprintf "Could not stop web server: %A" e)
