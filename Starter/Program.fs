open System
open System.IO
open System.Runtime.Serialization
open System.Security.Cryptography.X509Certificates
open WebServer
open System.Timers
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

let mutable serverSentEvent: SseContext option = None

let sseInit context =
    serverSentEvent <- Some context

let request (request: Request) = 
    let urlQuery = UrlQuery.create request.data.header.path
    let path = urlQuery.Query "path"
    let isVisble = urlQuery.Query "isVisible"
    Response.asyncSendJson request {
         name = "Uwe"
         email = "uriegel@hotmail.de"
         nothing = null
    } 

let onClosed id = 
    match serverSentEvent with
    | Some sse when sse.request.socketSessionId = id -> serverSentEvent <- None
    | _ -> ()

[<EntryPoint>]
let main argv =

    let timer = new Timer (6000.0)
    timer.Elapsed.Add (fun _ -> 
        match serverSentEvent with
        | Some sse -> sse.send "Left" "This is an event"
        |None -> ())
    timer.Start ()

    let certificateFile = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments), "certificate.pfx")
    let beits = Array.zeroCreate (int (FileInfo certificateFile).Length)
    use file = File.OpenRead certificateFile
    file.Read (beits, 0, beits.Length) |> ignore
    let certificate = Some (new X509Certificate2 (beits, "uwe"))
    // let certificate = Server.getCertificate "CAESAR"

    Logger.lowTraceEnabled <- true
    let configuration = { 
            Configuration.defaultConfiguration with 
                webroot = "../webroot"
                isTlsEnabled = true
                tlsRedirect = true
                tlsPort = 4433
                //Http2 = true
                certificate = certificate
                domainName = "uriegel.de"
                //domainName = "cas-ws121013.caseris.intern"                
                checkRequest = checkRequest
                request = request
                sessionClosed = Some onClosed
                serverSentEvent = Some sseInit
        }
    Server.start configuration

    printfn "Press any key to stop..."
    Console.ReadLine () |> ignore
    Server.stop ()


    0 
