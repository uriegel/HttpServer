using System;
using System.Collections.Generic;
using System.Text;

namespace HttpServer.Enums
{
    [Flags]
    public enum HeadersFlags : byte
    {
        NotSet = 0x0,
        /// <summary>
        /// Indicates this is the frame in the stream.
        /// </summary>
        END_STREAM = 0x1,
        /// <summary>
        /// Indicates this is the last HEADERS frame in the stream. If this is flag not set it implies a CONTINUATION frame is next.
        /// </summary>
        END_HEADERS = 0x4,
        /// <summary>
        /// Indicates that the Pad Length and Padding fields are used.
        /// </summary>
        PADDED = 0x8,
        /// <summary>
        /// When set it indicates that the E, Stream Dependency, and weight fields are used.
        /// </summary>
        PRIORITY = 0x20
    }
}
