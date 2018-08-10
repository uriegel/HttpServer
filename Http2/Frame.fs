namespace Http2

open System

type FrameType =
    /// Carries the core content for a stream
    DATA = 0x0uy
    /// Contains the HTTP headers and, optionally, priorities
    | HEADERS = 0x1uy
    /// Indicates or changes the stream priority and dependencies
    | PRIORITY = 0x2uy
    ///  Allows an endpoint to end a stream (generally an error case)
    | RST_STREAM = 0x3uy
    /// Communicates connection-level parameters
    | SETTINGS = 0x4uy
    /// Indicates to a client that a server is about to send something
    | PUSH_PROMISE = 0x5uy
    /// Tests connectivity and measures round-trip time (RTT)
    | PING = 0x6uy
    /// Tells an endpoint that the peer is done accepting new streams
    | GOAWAY = 0x7uy
    /// Communicates how many bytes an endpoint is willing to receive(used for flow control)
    | WINDOW_UPDATE = 0x8uy
    /// Used to extend HEADER blocks
    | CONTINUATION = 0x9uy 

type SettingsFlags =
    NotSet = 0x0uy
    | Ack = 0x1uy

type Frame(header: byte[], payload: byte[]) = 

    static member SIZE = 9

    member this.Length 
        with get() = payload.Length
    member this.Type  
        with get() = LanguagePrimitives.EnumOfValue<byte, FrameType> header.[3]
    member this.RawFlags
        with get() = header.[4]
    member this.StreamId
        with get() = BitConverter.ToInt32 ([| header.[8]; header.[7]; header.[6]; (header.[5] &&& ~~~0x1uy) |], 0)

type Settings(header: byte[], payload: byte[]) =
    inherit Frame (header, payload)

    member this.Flags  
        with get() = LanguagePrimitives.EnumOfValue<byte, SettingsFlags> this.RawFlags
    
    //let inline toMap kvps =
    //kvps
    //|> Seq.map (|KeyValue|)
    //|> Map.ofSeq