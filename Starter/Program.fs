open System
open System.IO
open System.Security.Cryptography.X509Certificates
open WebServer

[<EntryPoint>]
let main argv =
    let certificateFile = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), "certificate.pfx")
    let beits = Array.zeroCreate (int (FileInfo certificateFile).Length)
    use file = File.OpenRead certificateFile
    file.Read (beits, 0, beits.Length) |> ignore
    let certificate = new X509Certificate2 (beits, "uwe")

    Logger.lowTraceEnabled <- true
    let configuration = { 
            Configuration.defaultConfiguration with 
                //Webroot = @"C:\Users\urieg\source\repos\ingorico\ingorico"
                //Webroot = "/home/pi/test/WebServer/web/Reitbeteiligung"
                Webroot = @"C:\Program Files\caesar\CAEWebSrv\web"
                IsTlsEnabled = true
                TlsRedirect = true
                //TlsPort = 4433
                //Http2 = true
//CertificateName = "CAESAR"
                Certificate = Some certificate
                //DomainName = "uriegel.de"
                DomainName = "cas-ws121013.caseris.intern"                
        }
    Server.Start configuration
    printfn "Press any key to stop..."
    Console.ReadLine () |> ignore
    Server.Stop ()

    0 
