namespace Http2

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

type Frame(header: int) = 

    static member SIZE = 9

