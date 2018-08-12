namespace Http2

open System
open System.IO

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

type PingFlags =
    NotSet = 0x0uy
    | Ack = 0x1uy

[<System.FlagsAttribute>]
type SettingsIdentifier = 
    /// Changes the maximum size of the header table used for HPACK. Default: 4096
    HEADER_TABLE_SIZE = 0x1us
    /// If set to 0 the peer may not send a PUSH_PROMISE frame. Default: 1
    | ENABLE_PUSH = 0x2us
    /// Indicates the maximum number of streams that the sender will allow. Default: No limits
    | MAX_CONCURRENT_STREAMS = 0x3us
    /// Indicates the sender’s initial window size for flow control. Default: 65353
    | INITIAL_WINDOW_SIZE = 0x4us
    /// Indicates the maximum frame size the sender is willing to receive. This value must be between this initial value and 16,777,215 (224-1).Default: 16384
    | MAX_FRAME_SIZE = 0x5us
    /// No limit This setting is used to advise a peer of the maximum size of the header the sender is willing to accept. Default: No limits
    | MAX_HEADER_LIST_SIZE = 0x6us
    
[<System.FlagsAttribute>]
type HeadersFlags =
    NotSet = 0x0uy
    /// Indicates this is the frame in the stream.
    | END_STREAM = 0x1uy
    /// Indicates this is the last HEADERS frame in the stream. If this is flag not set it implies a CONTINUATION frame is next.
    | END_HEADERS = 0x4uy
    /// Indicates that the Pad Length and Padding fields are used.
    | PADDED = 0x8uy
    /// When set it indicates that the E, Stream Dependency, and weight fields are used.
    | PRIORITY = 0x20uy

type Frame(header: byte[], payload: byte[]) = 

    static member SIZE = 9

    member this.Length 
        with get () = payload.Length
    member this.Type  
        with get () = LanguagePrimitives.EnumOfValue<byte, FrameType> header.[3]
    member this.RawFlags
        with get () = header.[4]
    member this.StreamId
        with get () = BitConverter.ToInt32 ([| header.[8]; header.[7]; header.[6]; (header.[5] &&& ~~~0x1uy) |], 0)

    member this.serialize () =
        let result = Array.zeroCreate (header.Length + payload.Length)
        Array.Copy (header, result, header.Length)
        if payload.Length > 0 then Array.Copy (payload, 0, result, header.Length, payload.Length)
        result

type Settings(header: byte[], payload: byte[]) =
    inherit Frame (header, payload)

    static member createAck (streamId: int) =
        let streamIdBytes = BitConverter.GetBytes streamId
        let header = [| 0uy; 0uy; 0uy; byte FrameType.SETTINGS; byte SettingsFlags.Ack; streamIdBytes.[3] &&& ~~~1uy; streamIdBytes.[2]; streamIdBytes.[1]; streamIdBytes.[0] |]
        Settings (header, Array.zeroCreate 0) 

    member this.Values = 
        [0..(payload.Length - 1) / 6]
        |> Seq.mapi (fun i _ ->
            let id = BitConverter.ToUInt16 ([| payload.[(i * 6) + 1]; payload.[i * 6] |], 0)
            let value = BitConverter.ToInt32 ([| payload.[(i * 6) + 5]; payload.[(i * 6) + 4]; payload.[(i * 6) + 3]; payload.[(i * 6) + 2] |], 0)
            (LanguagePrimitives.EnumOfValue<uint16, SettingsIdentifier> id, value)
        )
        |> Map.ofSeq
    
    member this.Flags  
        with get () = LanguagePrimitives.EnumOfValue<byte, SettingsFlags> this.RawFlags

type WindowUpdate(header: byte[], payload: byte[]) =
    inherit Frame (header, payload)

    member this.SizeIncrement
        with get () = BitConverter.ToInt32 ([| payload.[3]; payload.[2]; payload.[1]; payload.[0] &&& ~~~1uy |], 0)
    
type Headers(header: byte[], payload: byte[]) =
    inherit Frame (header, payload)

    let getOffset flags =
        let mutable offset = 0
        if flags &&& HeadersFlags.PADDED = HeadersFlags.PADDED then
            offset <- 1
        if flags &&& HeadersFlags.PRIORITY = HeadersFlags.PRIORITY then 
            offset <- offset + 4
        offset

    member this.Flags  
        with get () = LanguagePrimitives.EnumOfValue<byte, HeadersFlags> this.RawFlags
    member this.PadLength 
        with get () = 
            match this.Flags with
            | _ when this.Flags &&& HeadersFlags.PADDED = HeadersFlags.PADDED -> payload.[0]
            | _ -> 0uy
    member this.E 
        with get () = 
            match this.Flags with
            | _ when this.Flags &&& HeadersFlags.PRIORITY = HeadersFlags.PRIORITY -> payload.[1] &&& 1uy <> 0uy
            | _ -> false
    member this.StreamDependency
        with get () = 
            match this.Flags with
            | _ when this.Flags &&& HeadersFlags.PRIORITY = HeadersFlags.PRIORITY ->
                BitConverter.ToInt32 ([| payload.[4]; payload.[3]; payload.[2]; payload.[1] &&& ~~~1uy |], 0)
            |_ -> 0
    member this.Weight
        with get () = payload.[getOffset this.Flags]
    member this.Stream  
        with get () =
            let padding = if this.Flags &&& HeadersFlags.PADDED = HeadersFlags.PADDED then this.PadLength else 0uy
            new MemoryStream (payload, 1 + getOffset this.Flags, payload.Length - 1 - getOffset this.Flags - int padding)

    static member create (streamId: int) (encodedHeaderFields: byte[]) (endFrame: bool) = 
        let payload = Array.zeroCreate (encodedHeaderFields.Length + 1)
        let bytes = BitConverter.GetBytes payload.Length
        let streamIdBytes = BitConverter.GetBytes streamId
        let headers = [| bytes.[2]; bytes.[1]; bytes.[0]; 
            byte FrameType.HEADERS; byte HeadersFlags.END_HEADERS;
            streamIdBytes.[3] &&& ~~~1uy; streamIdBytes.[2]; streamIdBytes.[1]; streamIdBytes.[0] 
        |]
        System.Array.Copy(encodedHeaderFields, 0, payload, 1, payload.Length - 1)
        Headers (headers, payload)

type RstStream(header: byte[], payload: byte[]) =
    inherit Frame (header, payload)

    member this.Error
        with get () = BitConverter.ToInt32 ([| payload.[3]; payload.[2]; payload.[1]; payload.[0] |], 0)

type Ping(header: byte[], payload: byte[]) =
    inherit Frame (header, payload)

    member this.Flags  
        with get () = LanguagePrimitives.EnumOfValue<byte, PingFlags> this.RawFlags

    member this.createAck () =
        header.[4] <- 1uy
        this
        
