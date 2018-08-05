using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer.Enums;
using HttpServer.Interfaces;

namespace HttpServer.Headers
{
    class ServerResponseHeaders
    {
        public ServerResponseHeaders(Server server, string sessionId)
        {
            this.server = server;
            this.sessionId = sessionId;
        }

        public int Status { get; set; } = 200;
        public string StatusDescription { get; set; } = "OK";
        public int ContentLength
        {
            get
            {
                if (_ContentLength == null)
                {
                    if (headers.ContainsKey("Content-Length"))
                    {
                        var val = -1;
                        int.TryParse(headers["Content-Length"], out val);
                        _ContentLength = val;
                    }
                }
                return _ContentLength ?? 0;
            }
            private set
            {
                _ContentLength = value;
            }
        }
        int? _ContentLength;

        public bool CloseConnection
        {
            get => (headers.ContainsKey("Connection") ? String.Compare(headers["Connection"], "close", true) == 0 : false);
        }

        public void Initialize(string contentType, int contentLength, string lastModified, bool noCache)
        {
            ContentLength = contentLength;
            if (contentType == "video/mp4")
            {
                // TODO: Wahrscheinlich unnötig, da SendMp4
                Add("ETag", "\"0815\"");
                Add("Accept-Ranges", "bytes");
                Add("Content-Type", contentType);
                Add("Keep-Alive", "timeout = 5, max = 99");
                Add("Connection", "Keep-Alive");
            }
            else
            {
                if (contentType != null)
                    Add("Content-Type", contentType);
                if (!string.IsNullOrEmpty(lastModified))
                    Add("Last-Modified", lastModified);
            }
            Add("Content-Length", $"{contentLength}");

            if (noCache)
            {
                Add("Cache-Control", "no-cache,no-store");
                Add("Expires", (DateTime.Now.Subtract(new TimeSpan(1, 0, 0))).ToUniversalTime().ToString("r"));
            }
        }

        public void InitializeJson(int contentLength)
        {
            ContentLength = contentLength;
            Add("Content-Length", $"{contentLength}");
            Add("Content-Type", "application/json; charset=UTF-8");
            Add("Cache-Control", "no-cache,no-store");
        }

        public void Add(string key, string value)
        {
            if (key == "Content-Length")
            {
                int val = -1;
                int.TryParse(value, out val);
                ContentLength = val;
            }
            headers[key] = value;
        }

        public bool Has(string key)
        {
            return headers.ContainsKey(key);
        }

        public void SetInfo(int status, int contentLength)
        {
            ContentLength = contentLength;
            Status = status;
        }

        public byte[] Access(bool isHttps, string httpResponseString, IRequestHeaders requestHeaders, byte[] payload = null)
        {
            if (!headers.ContainsKey("Content-Length"))
                headers["Connection"] = "close";

            headers["Date"] = DateTime.Now.ToUniversalTime().ToString("R");
            headers["Server"] = Constants.Server;

            if (server.Configuration.XFrameOptions != XFrameOptions.NotSet)
                headers["X-Frame-Options"] = server.Configuration.XFrameOptions.ToString();
            if (server.Configuration.HstsDurationInSeconds > 0)
                headers["Strict-Transport-Security"] = $"max-age={server.Configuration.HstsDurationInSeconds}";

            if (server.Configuration.AllowOrigins != null)
            {
                var origin = requestHeaders["origin"];
                if (!string.IsNullOrEmpty(origin))
                {
                    var host = requestHeaders["host"];
                    if (string.Compare(origin, host, true) != 0)
                    {
                        var originToAllow = server.Configuration.AllowOrigins.FirstOrDefault(n => string.Compare(n, origin, true) == 0);
                        if (originToAllow != null)
                            headers["Access-Control-Allow-Origin"] = originToAllow;
                    }
                }
            }

            if (requestHeaders.Method == Method.OPTIONS && headers.ContainsKey("Access-Control-Allow-Origin"))
            {
                var request = requestHeaders["Access-Control-Request-Headers"];
                if (request != null)
                    headers["Access-Control-Allow-Headers"] = request;
                request = requestHeaders["Access-Control-Request-Method"];
                if (request != null)
                    headers["Access-Control-Allow-Method"] = request;
            }

            var headerLines = headers.Select(n => $"{n.Key}: {n.Value}");
            var headerString = $"{httpResponseString} {Status} {StatusDescription}\r\n" + string.Join("\r\n", headerLines) + "\r\n\r\n";

            if (server.Configuration.HeaderTracing && Logger.Current.MinLogLevel == Logger.LogLevel.LowTrace)
            {
                Logger.Current.Trace($"{sessionId} Sending response");
                var headers = headerString.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var h in headers)
                    Logger.Current.Trace($"{sessionId} {h}");
            }

            if (payload == null)
                return ASCIIEncoding.ASCII.GetBytes(headerString);
            else
            {
                var result = new byte[ASCIIEncoding.ASCII.GetByteCount(headerString) + payload.Length];
                var headerBytes = ASCIIEncoding.ASCII.GetBytes(headerString, 0, headerString.Length, result, 0);
                Array.Copy(payload, 0, result, headerBytes, payload.Length);
                return result;
            }
        }

        Server server;
        readonly string sessionId;
        Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
