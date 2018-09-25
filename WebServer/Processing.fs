namespace WebServer

open System.Net.Sockets
open System.Threading
open System.Security.Authentication
open System.IO
open System
open System.Net.Security
open System.Security.Cryptography.X509Certificates
open Microsoft.Extensions.Logging
open System.Net
open System.Diagnostics

module Processing = 
    let mutable private idSeed = 0

    let asyncStartReceiving (tcpClient: TcpClient) isSecure (stopwatch: Stopwatch) = 
        let id = Interlocked.Increment &idSeed
        let log = Logger.log <| string id
        let lowTrace = Logger.lowTrace <| string id
        
        let asyncGetTlsNetworkStream () =
            async {
                let stream = tcpClient.GetStream ()
                let sslStream = new SslStream (stream)
                let authOptions = 
                    new SslServerAuthenticationOptions (ApplicationProtocols = System.Collections.Generic.List<SslApplicationProtocol>(),
                        EnabledSslProtocols = SslProtocols.Tls12,
                        AllowRenegotiation = false,
                        CertificateRevocationCheckMode = 
                            if Configuration.Current.CheckRevocation then 
                                X509RevocationMode.Online 
                            else 
                                X509RevocationMode.NoCheck
                        ,
                        ClientCertificateRequired = false,
                        EncryptionPolicy = EncryptionPolicy.RequireEncryption,
                        ServerCertificate = Configuration.Current.Certificate.Value,
                        ServerCertificateSelectionCallback = null)
                if Configuration.Current.Http2 then
                    authOptions.ApplicationProtocols.Add SslApplicationProtocol.Http2
                authOptions.ApplicationProtocols.Add SslApplicationProtocol.Http11
                do! sslStream.AuthenticateAsServerAsync (authOptions, CancellationToken false) |> Async.AwaitTask

                lowTrace (fun () ->
                    let getKeyExchangeAlgorithm () = 
                        if (int sslStream.KeyExchangeAlgorithm) = 44550 then "ECDHE" else sprintf "%A" sslStream.KeyExchangeAlgorithm

                    let getHashAlgorithm () =
                        match (int) sslStream.HashAlgorithm with
                        | 32781 -> "SHA384"
                        | 32780 -> "SHA256"
                        | _ -> sprintf "%A" sslStream.HashAlgorithm

                    sprintf "%d - secure protocol: %A\r\ncipher: %A\r\nstrength: %A\r\nkey exchange: %s\r\nstrength: %A\r\nhash: %s\r\nstrength: %A\r\napplication protocol: %A" 
                        id sslStream.SslProtocol sslStream.CipherAlgorithm sslStream.CipherStrength (getKeyExchangeAlgorithm ())
                        sslStream.KeyExchangeStrength (getHashAlgorithm ()) sslStream.HashStrength sslStream.NegotiatedApplicationProtocol
                )

                let http2 = sslStream.NegotiatedApplicationProtocol = SslApplicationProtocol.Http2
                return (sslStream :> Stream, http2)
            }
        
        async {
            lowTrace (fun () -> sprintf "New %ssocket session created: - %A" (if isSecure then "secure " else "") tcpClient.Client.RemoteEndPoint)
            // TODO: Counter erhöhen
            tcpClient.ReceiveTimeout <- Configuration.Current.SocketTimeout
            tcpClient.SendTimeout <- Configuration.Current.SocketTimeout
        
            let! (networkStream, http2) = 
                if isSecure then 
                    asyncGetTlsNetworkStream ()
                else
                    async { return (tcpClient.GetStream () :> Stream, false) }
            try
                let rec asyncReceive () = 
                    let requestStopwatch = Stopwatch ()
                    async {
                        if stopwatch.IsRunning then 
                            let elapsed = stopwatch.Elapsed
                            lowTrace (fun () -> sprintf "Connection established in %A" elapsed)
                            stopwatch.Stop ()
                        let! result = 
                            if http2 then 
                                RequestSession.asyncStart id networkStream stopwatch
                            else 
                                Request11Session.asyncStart {
                                    id = id
                                    remoteEndPoint = tcpClient.Client.RemoteEndPoint :?> IPEndPoint
                                    isSecure = isSecure
                                } networkStream requestStopwatch
                        if result then
                            return! asyncReceive ()
                    }
        
                do! asyncReceive () 
            with
            | :? AuthenticationException as e -> 
                log LogLevel.Warning <| sprintf "An authentication error has occurred while reading socket, session: %A, error: %A" tcpClient.Client.RemoteEndPoint e
            | :? IOException
            // TODO
            //| :? ConnectionClosedException as e
            | :? SocketException as e ->
                lowTrace <| fun () -> sprintf "Closing socket session, reason: %A" e
            | :? ObjectDisposedException ->
                lowTrace <| fun () -> "Object disposed"
            | e -> log LogLevel.Warning <| sprintf "An error has occurred while reading socket, error: %A" e
            tcpClient.Close ()
            // TODO: Counter erniedrigen
        }        


