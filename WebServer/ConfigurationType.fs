namespace WebServer

open System.Net
open System.Security.Cryptography.X509Certificates
open System.Security.Authentication

type XFrameOptions =
    NotSet = 0
    | DENY = 1
    | SAMEORIGIN= 2

type ConfigurationType = {
    localAddress: IPAddress
    webroot: string
    socketTimeout: int
    domainName: string
    //member val AllowOrigins = .Array<string>[0] { get; set; }
    port: int
    tlsPort: int
    isTlsEnabled: bool
    tlsRedirect: bool
    http2: bool
    certificate: X509Certificate2 Option
    checkRevocation: bool
    //member val  public string[] AppCaches { get; set; }
    xFrameOptions: XFrameOptions
    tlsProtocols: SslProtocols
    checkRequest: RequestHeaders->bool
    request: Request->Async<unit>
    serverSentEvent: (Request->unit) option
}

