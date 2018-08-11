namespace WebServer

open System.Collections.Generic
open System.IO
open System.Net
open System.Security.Cryptography.X509Certificates
open System.Security.Authentication

type TlsProtocol =
    Tls10 = 0
    | Tls11 = 1
    | Tls12 = 2

type XFrameOptions =
    NotSet = 0
    | DENY = 1
    | SAMEORIGIN= 2

type Configuration() =
    
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
