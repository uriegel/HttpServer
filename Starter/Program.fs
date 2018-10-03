﻿open System
open System.IO
open System.Runtime.Serialization
open System.Security.Cryptography.X509Certificates
open WebServer

let checkRequest requestHeaders = requestHeaders.path.StartsWith ("/Commander")

[<DataContract>]
type Affe = {
    [<field: DataMember(Name="name")>]
    name: string
    [<field: DataMember(Name="email")>]
    email: string
    [<field: DataMember(Name="nothing", EmitDefaultValue=false)>]
    nothing: string
}

let request (request: Request) = 
    let urlQuery = UrlQuery.create request.header.path
    let path = urlQuery.Query "path"
    let isVisble = urlQuery.Query "isVisible"
    Response.asyncSendJson request ({
         name = "Uwe"
         email = "uriegel@hotmail.de"
         nothing = null
     } :> obj)

[<EntryPoint>]
let main argv =
    let certificateFile = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "certificate.pfx")
    let beits = Array.zeroCreate (int (FileInfo certificateFile).Length)
    use file = File.OpenRead certificateFile
    file.Read (beits, 0, beits.Length) |> ignore
    let certificate = Some (new X509Certificate2 (beits, "uwe"))
    //let certificate = Server.getCertificate "CAESAR"

    Logger.lowTraceEnabled <- true
    let configuration = { 
            Configuration.defaultConfiguration with 
                webroot = "../webroot"
                //IsTlsEnabled = true
                tlsRedirect = true
                //TlsPort = 4433
                //Http2 = true
                certificate = certificate
                //DomainName = "uriegel.de"
                domainName = "cas-ws121013.caseris.intern"                
                checkRequest = checkRequest
                request = request
        }
    Server.Start configuration
    printfn "Press any key to stop..."
    Console.ReadLine () |> ignore
    Server.Stop ()

    0 
