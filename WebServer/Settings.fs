namespace WebServer

open System.Net
open System.Security.Cryptography.X509Certificates
open System.Security.Authentication

type XFrameOptions =
    NotSet = 0
    | DENY = 1
    | SAMEORIGIN= 2

type SettingsData = {
    LocalAddress: IPAddress
    Webroot: string
    SocketTimeout: int
    Extensions: int list
    DomainName: string
    //member val AllowOrigins = .Array<string>[0] { get; set; }
    Port: int
    TlsPort: int
    IsTlsEnabled: bool
    TlsTracing: bool
    TlsRedirect: bool
    Certificate: X509Certificate2
    CheckRevocation: bool
    //member val  public string[] AppCaches { get; set; }
    CertificateName: string
    HstsDurationInSeconds: int
    XFrameOptions: XFrameOptions
    TlsProtocols: SslProtocols
}

type Settings() = 
    static member val private current = Settings.createDefault ()
        with get, set

    static member private createDefault () = {
        LocalAddress = IPAddress.Any
        Webroot = ""
        SocketTimeout = 20000
        Extensions = []
        DomainName = ""
        //member val AllowOrigins = .Array<string>[0] { get; set; }
        Port = 80
        TlsPort = 443
        IsTlsEnabled = false
        TlsTracing = false
        TlsRedirect = false
        Certificate = null
        CheckRevocation = false
        //member val  public string[] AppCaches { get; set; }
        CertificateName = ""
        HstsDurationInSeconds = 0
        XFrameOptions = XFrameOptions.NotSet
        TlsProtocols = SslProtocols.Tls ||| SslProtocols.Tls11 ||| SslProtocols.Tls12
    }
 
    static member Current 
        with get() = Settings.current

    static member Initialize settingsData = 
        Settings.current <- settingsData

