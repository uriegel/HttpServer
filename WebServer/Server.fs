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
   
    let internal initialize () = 
        ServicePointManager.DefaultConnectionLimit <- 1000 
        ServicePointManager.SecurityProtocol <- SecurityProtocolType.Tls12 ||| SecurityProtocolType.Tls11 ||| SecurityProtocolType.Tls 
        ThreadPool.SetMinThreads (1000, 1000) |> ignore
        true
    
    let isInitialized = initialize ()

    let private toSettings (configuration: InitializationData) = {
        LocalAddress = configuration.LocalAddress
        Webroot = configuration.Webroot
        SocketTimeout = configuration.SocketTimeout
        Extensions = List.ofSeq configuration.Extensions
        DomainName = configuration.DomainName
        //member val AllowOrigins = .Array<string>[0] { get; set; }
        Port = configuration.Port
        TlsPort = configuration.TlsPort
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
    
    let Start (configuration: InitializationData) = 
        let isInitialized = isInitialized
        Settings.Initialize (toSettings configuration)
        Logger.Info "Starting Web Server"
        
        Logger.Info "Web Server started"

    let Stop () =
        ()
