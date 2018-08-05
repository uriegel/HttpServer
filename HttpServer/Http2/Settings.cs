﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HttpServer.Enums;

namespace HttpServer.Http2
{
    class Settings : Frame
    {
        enum Identifier : short
        {
            /// <summary>
            /// Changes the maximum size of the header table used for HPACK. Default: 4096
            /// </summary>
            SETTINGS_HEADER_TABLE_SIZE = 0x1 ,
            /// <summary>
            /// If set to 0 the peer may not send a PUSH_PROMISE frame. Default: 1
            /// </summary>
            SETTINGS_ENABLE_PUSH = 0x2,
            /// <summary>
            /// Indicates the maximum number of streams that the sender will allow. Default: No limits
            /// </summary>
            SETTINGS_MAX_CONCURRENT_STREAMS = 0x3,
            /// <summary>
            /// Indicates the sender’s initial window size for flow control. Default: 65353
            /// </summary>
            SETTINGS_INITIAL_WINDOW_SIZE = 0x4,
            /// <summary>
            /// Indicates the maximum frame size the sender is willing to receive. This value must be between this initial value and 16,777,215 (224-1).Default: 16384
            /// </summary>
            SETTINGS_MAX_FRAME_SIZE = 0x5,
            /// <summary>
            /// No limit This setting is used to advise a peer of the maximum size of the header the sender is willing to accept. Default: No limits
            /// </summary>
            SETTINGS_MAX_HEADER_LIST_SIZE = 0x6 
        }

        public SettingsFlags Flags { get => (SettingsFlags)flags; }

        public Settings(int length, Type type, SettingsFlags flags, long streamId, byte[] payload)
            : base(length, type, (byte)flags, streamId, payload)
        {
            if (payload != null)
            {
                var count = (payload.Length) / 6;
                values = Enumerable.Repeat(0, count).Select((_, i) =>
                {
                    var shortValue = new byte[2];
                    shortValue[0] = payload[(i * 6) + 1];
                    shortValue[1] = payload[(i * 6)];
                    var id = (Identifier)BitConverter.ToInt16(shortValue);
                    var value = new byte[4];
                    value[0] = payload[(i * 6) + 5];
                    value[1] = payload[(i * 6) + 4];
                    value[2] = payload[(i * 6) + 3];
                    value[3] = payload[(i * 6) + 2];
                    var val = BitConverter.ToInt32(value);
                    return new KeyValuePair<Identifier, int>(id, val);
                }).ToDictionary(k => k.Key, v => v.Value);
            }
        }

        Dictionary<Identifier, int> values = new Dictionary<Identifier, int>();
    }
}