namespace WebServer

open System.Net
open System.Security.Cryptography.X509Certificates
open System.Security.Authentication

type XFrameOptions =
    NotSet = 0
    | DENY = 1
    | SAMEORIGIN= 2

// type ConfigurationData = {
//     LocalAddress: IPAddress
//     Webroot: string
//     SocketTimeout: int
//     Extensions: int list
//     DomainName: string
//     //member val AllowOrigins = .Array<string>[0] { get; set; }
//     Port: int
//     TlsPort: int
//     IsTlsEnabled: bool
//     TlsRedirect: bool
//     Http2: bool
//     Certificate: X509Certificate2 Option
//     CheckRevocation: bool
//     //member val  public string[] AppCaches { get; set; }
//     //HstsDurationInSeconds: int // Not with LetsEncrypt
//     XFrameOptions: XFrameOptions
//     TlsProtocols: SslProtocols
// }

type ConfigurationType = {
    LocalAddress: IPAddress
    Webroot: string
    SocketTimeout: int
    //Extensions = []
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
    // HstsDurationInSeconds = 0 // Not with LetsEncrypt
    XFrameOptions: XFrameOptions
    TlsProtocols: SslProtocols
}

