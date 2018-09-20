namespace WebServer

open System.Net
open System.Security.Cryptography.X509Certificates
open System.Security.Authentication

type XFrameOptions =
    NotSet = 0
    | DENY = 1
    | SAMEORIGIN= 2

type ConfigurationData = {
    LocalAddress: IPAddress
    Webroot: string
    SocketTimeout: int
    Extensions: int list
    DomainName: string
    //member val AllowOrigins = .Array<string>[0] { get; set; }
    Port: int
    TlsPort: int
    IsTlsEnabled: bool
    TlsRedirect: bool
    Http2: bool
    Certificate: X509Certificate2 Option
    CheckRevocation: bool
    //member val  public string[] AppCaches { get; set; }
    HstsDurationInSeconds: int
    XFrameOptions: XFrameOptions
    TlsProtocols: SslProtocols
}

type Configuration() = 
    static member val private current = Configuration.CreateDefault ()
        with get, set

    static member private CreateDefault () = {
        LocalAddress = IPAddress.Any
        Webroot = ""
        SocketTimeout = 20000
        Extensions = []
        DomainName = ""
        //member val AllowOrigins = .Array<string>[0] { get; set; }
        Port = 80
        TlsPort = 443
        IsTlsEnabled = false
        TlsRedirect = false
        Http2 = false
        Certificate = None
        CheckRevocation = false
        //member val  public string[] AppCaches { get; set; }
        HstsDurationInSeconds = 0
        XFrameOptions = XFrameOptions.NotSet
        TlsProtocols = SslProtocols.Tls ||| SslProtocols.Tls11 ||| SslProtocols.Tls12
    }
 
    static member Current 
        with get() = Configuration.current

    static member Initialize settingsData = 
        Configuration.current <- settingsData

