using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using HttpServer.Enums;
using HttpServer.Exceptions;
using HttpServer.Extensions;
using HttpServer.Headers;
using HttpServer.Interfaces;
using HttpServer.WebService;
using HttpServer.WebSockets;

namespace HttpServer.Sessions
{
    class RequestSession : ISession, IDisposable
    {
        #region Properties

        public string Id { get; }

        public IRequestHeaders Headers { get; private set; }
        public bool IsSecureConnection { get { return SocketSession?.UseTls ?? false; } }
        public static Counter Instances { get; } = new Counter();
        public Server Server { get; }
        public SocketSession SocketSession { get; }
        public IPEndPoint LocalEndPoint { get; private set; }
        public IPEndPoint RemoteEndPoint { get; private set; }
        public string UrlRoot
        {
            get
            {
                if (_UrlRoot == null)
                    _UrlRoot = $"http{(string)(Server.Configuration.IsTlsEnabled ? "s" : null)}://{Headers.Host}";
                return _UrlRoot;
            }
        }
        string _UrlRoot;
        /// <summary>
        /// Mit Hilfe einer ServerSupportFunction kann dem Webserver ein alternativer Pfad für die Datei mitgeteilt werden.
        /// Um diesen dann zu nutzen, muss dier Isapirequest mit 5 beendet werden. In der folgenden Verarbeitung wird dann dieser geänderte
        /// Pfad als ecb.PathTranslated verwendet
        /// </summary>
        public string TranslatedPathOverride { get; set; }
        public string HttpResponseString
        {
            get
            {
                if (_HttpResponseString == null)
                    _HttpResponseString = (Headers as RequestHeaders).Http10 ? "HTTP/1.0" : "HTTP/1.1";
                return _HttpResponseString;
            }
        }
        string _HttpResponseString;

        #endregion

        #region Constructor

        public RequestSession(Server server, SocketSession socketSession, Stream networkStream)
        {
            Id = socketSession.Id + "-" + Interlocked.Increment(ref idSeed);
            responseHeaders = new ServerResponseHeaders(server, Id);
            Instances.Increment();
            stopwatch = new Stopwatch();

            this.SocketSession = socketSession;
            this.Server = server;
            this.networkStream = networkStream;

            this.LocalEndPoint = SocketSession?.Client?.Client?.LocalEndPoint as IPEndPoint;
            this.RemoteEndPoint = SocketSession?.Client?.Client?.RemoteEndPoint as IPEndPoint;

            Logger.Current.LowTrace(() => $"{Id} New request session created - {RemoteEndPoint}");
        }

        #endregion

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True: In dieser SocketSession weitermachen, ansonsten Verarbeitung abbrechen</returns>
        public virtual async Task<bool> StartAsync()
        {
            try
            {
                var read = await networkStream.ReadAsync(readBuffer, 0, readBuffer.Length);
                if (read == 0)
                {
                    Logger.Current.LowTrace(() => $"{Id} Socket session closed");
                    return false;
                }
                return await ReceiveAsync(read);
            }
            catch (Exception e) when (e is IOException || e is ConnectionClosedException || e is SocketException)
            {
                Logger.Current.LowTrace(() => $"{Id} Closing socket session: {e}");
                Close(true);
                return false;
            }
            catch (Exception e) when (e is ObjectDisposedException)
            {
                Logger.Current.Trace($"{Id} Object disposed");
                Close(true);
                return false;
            }
            catch (Exception e)
            {
                Logger.Current.Warning($"{Id} An error has occurred while reading socket: {e}");
                Close(true);
                return false;
            }
            finally
            {
                Instances.DecrementActive();
            }
        }

        public async Task<Service> GetServiceInfoAsync()
            => (string.IsNullOrEmpty(Headers.ContentType)
                    && (Headers.ContentType.StartsWith("text/json", StringComparison.InvariantCultureIgnoreCase)
                    || Headers.ContentType.StartsWith("application/json", StringComparison.InvariantCultureIgnoreCase)))
            ? await Service.CreateAsync(this)
            : null;

        public bool CheckWsUpgrade()
        {
            var upgrade = Headers["upgrade"];
            return (upgrade != null) ? (string.Compare(upgrade, "websocket", true) == 0) : false;
        }

        public async Task<string> ReadStringAsync()
        {
            var ms = new MemoryStream();
            await ReadStreamAsync(ms);
            ms.Position = 0;
            var buffer = new byte[ms.Length];
            ms.Read(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        public async Task ReadStreamAsync(Stream stream)
        {
            var cls = Headers["content-length"];
            var length = int.Parse(cls ?? "0");

            while (length > 0)
            {
                int read;
                if (readFromBuffer)
                {
                    var cache = bufferReadCount - bufferEndPosition;
                    if (cache > 0)
                        read = Math.Min(length, cache);
                    else
                    {
                        readFromBuffer = false;
                        continue;
                    }
                }
                else
                {
                    var readLength = Math.Min(readBuffer.Length, length);
                    read = await networkStream.ReadAsync(readBuffer, 0, readLength);
                    if (read == 0 && readLength > 0)
                        throw new ConnectionClosedException();
                }
                length -= read;
                await stream.WriteAsync(readBuffer, readFromBuffer ? bufferEndPosition : 0, read);
                readFromBuffer = false;
            }
        }

        public async Task SendFileAsync(string file)
        {
            if (file.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase)
                || file.EndsWith(".mkv", StringComparison.InvariantCultureIgnoreCase)
                || file.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase)
                || file.EndsWith(".wav", StringComparison.InvariantCultureIgnoreCase))
                await SendRangeAsync(file);
            else
                await InternalSendFileAsync(file);
        }

        public async Task SendStreamAsync(Stream stream, string contentType, string lastModified, bool noCache)
        {
            if (!noCache)
            {
                string isModifiedSince = Headers["if-modified-since"];
                if (isModifiedSince == Constants.NotModified)
                {
                    await Send304Async();
                    return;
                }
            }

            if (Headers.ContentEncoding != ContentEncoding.None &&
                contentType != null &&
                (contentType.StartsWith("application/javascript", StringComparison.CurrentCultureIgnoreCase)
                    || contentType.StartsWith("text/", StringComparison.CurrentCultureIgnoreCase)))
            {
                var ms = new MemoryStream();

                Stream compressedStream;
                switch (Headers.ContentEncoding)
                {
                    case ContentEncoding.Deflate:
                        responseHeaders.Add("Content-Encoding", "deflate");
                        compressedStream = new DeflateStream(ms, System.IO.Compression.CompressionMode.Compress, true);
                        break;
                    case ContentEncoding.GZip:
                        responseHeaders.Add("Content-Encoding", "gzip");
                        compressedStream = new GZipStream(ms, System.IO.Compression.CompressionMode.Compress, true);
                        break;
                    default:
                        compressedStream = null;
                        break;
                }
                using (compressedStream)
                {
                    stream.CopyTo(compressedStream);
                    compressedStream.Close();
                    stream = ms;
                }
                ms.Position = 0;
            }

            responseHeaders.Initialize(contentType, (int)stream.Length, lastModified, noCache);

            if (contentType != null &&
                (contentType.StartsWith("application/javascript", StringComparison.CurrentCultureIgnoreCase)
                || contentType.StartsWith("text/css", StringComparison.CurrentCultureIgnoreCase)
                || contentType.StartsWith("text/html", StringComparison.CurrentCultureIgnoreCase)))
            {
                responseHeaders.Add("Expires", DateTime.Now.ToUniversalTime().ToString("r"));
                //responseHeaders.Add("Cache-Control", "must-revalidate");
                //responseHeaders.Add("Expires", "-1");
            }

            var headerBuffer = responseHeaders.Access(SocketSession.UseTls, HttpResponseString, Headers);
            await WriteAsync(headerBuffer, 0, headerBuffer.Length);

            if (Headers.Method == Method.HEAD)
                return;

            var bytes = new byte[8192];
            while (true)
            {
                var read = await stream.ReadAsync(bytes, 0, bytes.Length);
                if (read == 0)
                    return;
                await WriteAsync(bytes, 0, read);
            }
        }

        public async Task Send304Async()
        {
            var headerString = $"{HttpResponseString} 304 Not Modified\r\n\r\n";
            Logger.Current.LowTrace(() => $"{Id} {headerString}");

            var vorspannBuffer = ASCIIEncoding.ASCII.GetBytes(headerString);
            responseHeaders.SetInfo(304, 0);
            await WriteAsync(vorspannBuffer, 0, vorspannBuffer.Length);
        }

        public async Task SendOKAsync(string responseText)
        {
            var responseBytes = Encoding.UTF8.GetBytes(responseText);
            responseHeaders.Add("Content-Length", $"{responseBytes.Length}");
            responseHeaders.Add("Content-Type", "text/html; charset=UTF-8");
            var headerBuffer = responseHeaders.Access(SocketSession.UseTls, HttpResponseString, Headers);
            await WriteAsync(headerBuffer, 0, headerBuffer.Length);
            await WriteAsync(responseBytes, 0, responseBytes.Length);
        }

        public async Task SendErrorAsync(string htmlHead, string htmlBody, int errorCode, string errorText)
        {
            var response = $"<html><head>{htmlHead}</head><body>{htmlBody}</body></html>";
            var responseBytes = Encoding.UTF8.GetBytes(response);

            responseHeaders.Status = errorCode;
            responseHeaders.StatusDescription = errorText;
            responseHeaders.Add("Content-Length", $"{responseBytes.Length}");
            responseHeaders.Add("Content-Type", "text/html; charset=UTF-8");
            var headerBuffer = responseHeaders.Access(SocketSession.UseTls, HttpResponseString, Headers);
            await WriteAsync(headerBuffer, 0, headerBuffer.Length);
            await WriteAsync(responseBytes, 0, responseBytes.Length);
        }

        public async Task SendExceptionAsync(Exception e)
        {
            var htmlBody = "";

            var exception = e;
            while (null != exception)
            {
                htmlBody += $"<div>{HttpUtility.HtmlEncode(exception.Message)}</div><pre>{HttpUtility.HtmlEncode(exception.StackTrace)}</pre>";
                exception = exception.InnerException;
            }

            await SendErrorAsync(
@"<title>CAESAR</title>
<Style> 
html {
    font-family: sans-serif;
}
h1 {
    font-weight: 100;
}
</Style>",
                    $"<h1>Internal server error</h1>{htmlBody}",
                    500, "Internal server error");
        }

        public void Close() => Close(false);

        public void Close(bool fullClose)
        {
            try
            {
                if (fullClose)
                {
                    networkStream.Close();
                    isClosed = true;
                }
                else
                    SocketSession.Client.Client.Shutdown(SocketShutdown.Send);
            }
            catch { }
        }

        public async Task WriteAsync(byte[] buffer, int offset, int length)
            => await networkStream.WriteAsync(buffer, offset, length);

        public async Task WriteStreamAsync(Stream stream)
        {
            var bytes = new byte[8192];
            while (true)
            {
                var read = await stream.ReadAsync(bytes, 0, bytes.Length);
                if (read == 0)
                    return;
                await WriteAsync(bytes, 0, read);
            }
        }

        public async Task<WebSocketSession> UpgradeWebSocketAsync()
        {
            await UpgradeToWebSocketAsync();
            return new WebSocketSession(this, Server, Server.Configuration);
        }

        public async Task SendHtmlStringAsync(string html)
        {
            var bytes = Encoding.UTF8.GetBytes(html);
            responseHeaders.Add("Content-Length", $"{bytes.Length}");
            responseHeaders.Add("Content-Type", "text/html; charset=UTF-8");
            var headerBuffer = responseHeaders.Access(SocketSession.UseTls, HttpResponseString, Headers);
            await WriteAsync(headerBuffer, 0, headerBuffer.Length);
            await WriteAsync(bytes, 0, bytes.Length);
        }

        public async Task SendJsonBytesAsync(byte[] bytes)
        {
            var contentLength = bytes.Length;
            responseHeaders.InitializeJson(contentLength);
            switch (Headers.ContentEncoding)
            {
                case ContentEncoding.Deflate:
                    responseHeaders.Add("Content-Encoding", "deflate");
                    break;
                case ContentEncoding.GZip:
                    responseHeaders.Add("Content-Encoding", "gzip");
                    break;
                default:
                    break;
            }
            var tcpPayload = responseHeaders.Access(SocketSession.UseTls, HttpResponseString, Headers, bytes);
            await networkStream.WriteAsync(tcpPayload, 0, tcpPayload.Length);
        }

        public Task<bool> RedirectAsync(string url) => RedirectAsync(url, false);

        public Stream GetNetworkStream() => networkStream;

        async Task<bool> ReceiveAsync(int bufferPosition)
        {
            try
            {
                Headers = new RequestHeaders();
                var result = await (Headers as Headers.Headers).InitializeAsync(networkStream, readBuffer, bufferPosition, Server.Configuration.HeaderTracing, Id);
                bufferEndPosition = result.BufferEndPosition;
                readFromBuffer = bufferEndPosition > 0;
                bufferReadCount = result.BufferReadCount;

                Logger.Current.Trace($"{Id} Request: {RemoteEndPoint} \"{Headers.Method} {Headers.Url} {Headers.Http}\"");

                stopwatch?.Start();

                //string refererPath = null;
                //if (headers.ContainsKey("Referer"))
                //{
                //    string path = headers["Referer"];
                //    path = path.Substring(path.IndexOf("//") + 2);
                //    refererPath = path.Substring(path.IndexOf('/') + 1);
                //}

                if (Headers.Method == Method.OPTIONS)
                    return ServeOptions();

                string query = null;
                var extensionResult = CheckExtension(Headers.Url);
                if (extensionResult.HasValue)
                {
                    var path = extensionResult.Value.Url;
                    query = Headers.Url.Substring(path.Length).Trim('/');
                    return await extensionResult.Value.Extension.RequestAsync(this, Headers.Method, path, query) && !isClosed;
                }

                var isapi = CheckIsapi(Headers.Url);
                if (isapi != null)
                    if (await ThreadTask<bool>.RunAsync(() =>
                    {
                        var request = isapi.CreateRequest();
                        string path;
                        var pos = Headers.Url.IndexOf('?');
                        if (pos != -1)
                        {
                            query = Headers.Url.Substring(pos + 1);
                            path = Headers.Url.Substring(0, pos);
                        }
                        else
                            path = Headers.Url;

                        if (isapi.Url.Length == path.Length)
                            path = null;
                        else if (isapi.Url == "/" && (path?.StartsWith("/") ?? false))
                            path = path.Substring(1);

                        else if (isapi.Url.Length > 1 && isapi.Url.Length < path.Length)
                            path = path.Substring(isapi.Url.Length + 1);

                        if (!string.IsNullOrEmpty(path))
                            path = Uri.UnescapeDataString(path);

                        if (request.Request(this, Headers.Method, Headers.ContentType, TranslatedPathOverride, path, query))
                        {
                            request.SetInfo(responseHeaders);
                            return true;
                        }
                        return false;
                    }))
                        return true;

                var redirection = CheckRedirection(Headers.Url);
                if (redirection != null)
                {
                    if (redirection.IsProxy())
                        return await RedirectAsync(redirection.GetRedirectedUrl(Headers.Url));
                    else
                    {
                        await RedirectDirectoryAsync(redirection.GetRedirectedUrl(Headers.Url));
                        return true;
                    }
                }

                if (Headers.Method == Method.POST && Headers.ContentType == "application/x-www-form-urlencoded" ||
                    Headers.Method == Method.GET && Headers.Url.Contains('?'))
                {
                    var formHandler = CheckForm(Headers.Url);
                    if (formHandler != null)
                    {
                        var queryComponents = new UrlQueryComponents(Headers.Method == Method.POST ? $"{Headers.Url}?{await ReadStringAsync()}" : Headers.Url);
                        await SendHtmlStringAsync(await formHandler.OnSubmitAsync(this, Headers.Method, Headers.Url, queryComponents));
                        return true;
                    }
                }

                if (await SendInternalURLAsync(Headers.Url))
                    return true;

                if (!CheckFile(Headers.Url, out string redirURL, out string localFile, out string pathInfo, out string queryString))
                {
                    await SendNotFoundAsync();
                    return true;
                }

                if (!String.IsNullOrEmpty(redirURL))
                {
                    await RedirectDirectoryAsync(redirURL);
                    return true;
                }

                await SendFileAsync(localFile);
                return true;
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.TimedOut)
                {
                    Logger.Current.Warning($"{Id} Socket session closed, Timeout has occurred");
                    Close(true);
                    return false;
                }
                return true;
            }
            catch (ConnectionClosedException)
            {
                Logger.Current.LowTrace(() => $"{Id} Socket session closed via exception");
                Close(true);
                return false;
            }
            catch (ObjectDisposedException oe)
            {
                Logger.Current.Info($"{Id} Socket session closed, an error has occurred: {oe}");
                Close(true);
                return false;
            }
            catch (IOException ioe)
            {
                Logger.Current.LowTrace(() => $"{Id} Socket session closed: {ioe}");
                Close(true);
                return false;
            }
            catch (InvalidPathException)
            {
                Close(true);
                return false;
            }
            catch (Exception e)
            {
                Logger.Current.Warning($"{Id} Socket session closed, an error has occurred while receiving: {e}");
                Close(true);
                return false;
            }
            finally
            {
                var elapsed = stopwatch?.Elapsed;
                stopwatch?.Stop();
                Logger.Current.Trace($"{Id} Answer: {RemoteEndPoint} \"{Headers.Method} {Headers.Url} {Headers.Http}\" Status: {responseHeaders.Status} Size: {responseHeaders.ContentLength} Duration: {elapsed}");
            }
        }

        ExtensionResult? CheckExtension(string url)
        {
            return (from ext in Server.Configuration.Extensions
                    from extUrl in ext.Urls
                    where url.StartsWith(extUrl, StringComparison.CurrentCultureIgnoreCase)
                    select (ExtensionResult?)new ExtensionResult(extUrl, ext)).FirstOrDefault();
        }

        Redirection CheckRedirection(string url)
        {
            var pos = url.IndexOf('?');
            if (pos != -1)
                url = url.Substring(0, pos);
            return Server.Configuration.Redirections.FirstOrDefault(n => url.StartsWith(n.RedirectionBaseUrl, StringComparison.CurrentCultureIgnoreCase));
        }

        async Task SendRangeAsync(string file)
        {
            // TODO: umstellen auf ResponseHeader
            // TODO: mp4, mp3, ...
            var fi = new FileInfo(file);
            using (Stream stream = File.OpenRead(file))
                await SendRangeAsync(stream, fi.Length, file, null);
        }

        async Task SendRangeAsync(Stream stream, long fileLength, string file, string contentType)
        {
            string rangeString = Headers["range"];
            if (rangeString == null)
            {
                if (!string.IsNullOrEmpty(file))
                    await InternalSendFileAsync(file);
                else
                    await SendStreamAsync(stream, contentType, DateTime.Now.ToUniversalTime().ToString("r"), true);
                return;
            }

            rangeString = rangeString.Substring(rangeString.IndexOf("bytes=") + 6);
            int minus = rangeString.IndexOf('-');
            long start = 0;
            long end = fileLength - 1;
            if (minus == 0)
                end = long.Parse(rangeString.Substring(1));
            else if (minus == rangeString.Length - 1)
                start = long.Parse(rangeString.Substring(0, minus));
            else
            {
                start = long.Parse(rangeString.Substring(0, minus));
                end = long.Parse(rangeString.Substring(minus + 1));
            }

            var contentLength = end - start + 1;
            if (string.IsNullOrEmpty(contentType))
                contentType = "video/mp4";
            var headerString =
$@"{HttpResponseString} 206 Partial Content
ETag: ""0815""
Accept-Ranges: bytes
Content-Length: {contentLength}
Content-Range: bytes {start}-{end}/{fileLength}
Keep-Alive: timeout=5, max=99
Connection: Keep-Alive
Content-Type: {contentType}

";
            Logger.Current.LowTrace(() => $"{Id} {headerString}");
            var vorspannBuffer = ASCIIEncoding.ASCII.GetBytes(headerString);
            await networkStream.WriteAsync(vorspannBuffer, 0, vorspannBuffer.Length);
            var bytes = new byte[40000];
            var length = end - start;
            stream.Seek(start, SeekOrigin.Begin);
            long completeRead = 0;
            while (true)
            {
                var read = await stream.ReadAsync(bytes, 0, Math.Min(bytes.Length, (int)(contentLength - completeRead)));
                if (read == 0)
                    return;
                completeRead += read;
                await networkStream.WriteAsync(bytes, 0, read);
                if (completeRead == contentLength)
                    return;
            }
        }

        async Task<bool> RedirectAsync(string url, bool addXForwardedUri)
        {
            try
            {
                if (CheckWsUpgrade())
                {
                    var webSocketSession = await UpgradeWebSocketAsync();
                    var webSocket = new WebSocket(webSocketSession);
                    var webSocketProxy = new WebSocketProxy(webSocket, url);
                    return true;
                }

                if (Headers.Method != Method.GET && Headers.Method != Method.POST)
                    throw new Exception($"Proxy Redirection: {Headers.Method} not supported");

                var webRequest = (HttpWebRequest)WebRequest.Create(url);
                webRequest.Method = Headers.Method.ToString();

                var body = Headers.Method == Method.POST ? await GetAsync() : null;

                foreach (var h in Headers.Raw.Where(n => n.Key != "host"
                                    && n.Key != "user-agent"
                                    && n.Key != "referer"))
                {
                    switch (h.Key.ToLower())
                    {
                        case "accept":
                            webRequest.Accept = h.Value.Value;
                            break;
                        case "connection":
                            if (h.Value.Value != "Keep-Alive")
                                webRequest.KeepAlive = false;
                            break;
                        case "if-modified-since":
                            {
                                var dts = h.Value.Value;
                                var pos = dts.IndexOf(';');
                                if (pos != -1)
                                    dts = dts.Substring(0, pos);
                                var dt = DateTime.Parse(dts.Trim());
                                webRequest.IfModifiedSince = dt;
                            }
                            break;
                        case "content-length":
                            {
                                if (int.TryParse(h.Value.Value, out var cl))
                                    webRequest.ContentLength = cl;
                                else
                                    Logger.Current.Warning($"{Id} Could not set Content-Length");
                            }
                            break;
                        case "content-type":
                            webRequest.ContentType = h.Value.Value;
                            break;
                        case "host":
                            webRequest.Host = h.Value.Value;
                            break;
                        case "user-agent":
                            webRequest.UserAgent = h.Value.Value;
                            break;
                        case "range":
                            try
                            {
                                var sizes = h.Value.Value.Split(new[] { ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries).Skip(1)
                                    .Select(n => long.Parse(n)).ToArray();
                                if (sizes.Length > 1)
                                    webRequest.AddRange(sizes[0], sizes[1]);
                            }
                            catch (Exception e)
                            {
                                Logger.Current.Warning($"{Id} Error occurred in range: {e}");
                            }
                            break;
                        case "referer":
                            webRequest.Referer = h.Value.Value;
                            break;
                        default:
                            {
                                try
                                {
                                    webRequest.Headers.Add(h.Value.Key + ": " + h.Value.Value);
                                }
                                catch (Exception e)
                                {
                                    Logger.Current.Warning($"{Id} Could not redirect: {e}");
                                }
                            }
                            break;
                    }
                }
                if (addXForwardedUri)
                    webRequest.Headers.Add($"X-Forwarded-URI: {CreateXForwarded()}");

                // TODO: Header Tracing
                if (body != null)
                    using (var requestStream = await webRequest.GetRequestStreamAsync())
                        await requestStream.WriteAsync(body, 0, body.Length);

                HttpWebResponse response = null;
                try
                {
                    webRequest.CertificateValidator(e =>
                    {
                        Logger.Current.Warning($"{Id} {e.Message}");
                        e.ChainErrorDescriptions?.Perform(n =>
                        {
                            Logger.Current.Warning($"{Id} {n}");
                            return true;
                        });
                        return false;
                    });
                    response = (HttpWebResponse)await webRequest.GetResponseAsync();
                }
                catch (WebException we)
                {
                    if (we.Response == null)
                        throw we;
                    response = (HttpWebResponse)we.Response;
                }
                var strom = response.GetResponseStream();

                var responseHeaders = response.Headers.AllKeys.Select(n => string.Format("{0}: {1}", n, response.Headers[n]));
                // TODO: Header Tracing
                //if (Tracing.Current.IsEnabled && Tracing.Current.IsHttpHeaderEnabled)
                //    Tracing.Current.TraceGetResponseHeaders(responseHeaders);
                responseHeaders = responseHeaders.Where(n => !n.StartsWith("allow:", StringComparison.InvariantCultureIgnoreCase)
                    && !n.StartsWith("connection:", StringComparison.InvariantCultureIgnoreCase));
                var headerString = string.Join("\r\n", responseHeaders) + "\r\n\r\n";
                var html = $"{HttpResponseString} {(int)response.StatusCode} {response.StatusDescription}\r\n" + headerString;
                var htmlBytes = Encoding.UTF8.GetBytes(html);
                await WriteAsync(htmlBytes, 0, htmlBytes.Length);
                await WriteStreamAsync(strom);
                return true;
            }
            catch (Exception e)
            {
                Logger.Current.LowTrace(() => $"An error has occurred while redirecting: {e}");
                try
                {
                    await SendExceptionAsync(e);
                }
                catch { }
                return false;
            }
        }

        async Task<byte[]> GetAsync()
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    await ReadStreamAsync(ms);
                    ms.Position = 0;
                    var result = new byte[ms.Length];
                    ms.Read(result, 0, result.Length);
                    return result;
                }
            }
            catch (Exception e)
            {
                var was = e.ToString();
                throw;
            }
        }


        async Task InternalSendFileAsync(string file)
        {
            var fi = new FileInfo(file);
            var noCache = Server.Configuration.NoCacheFiles.Contains(file.ToLower());

            if (!noCache)
            {
                var isModifiedSince = Headers["if-modified-since"];
                if (isModifiedSince != null)
                {
                    var pos = isModifiedSince.IndexOf(';');
                    if (pos != -1)
                        isModifiedSince = isModifiedSince.Substring(0, pos);
                    var ifModifiedSince = Convert.ToDateTime(isModifiedSince);
                    var fileTime = fi.LastWriteTime.AddTicks(-(fi.LastWriteTime.Ticks % TimeSpan.FromSeconds(1).Ticks));
                    var diff = fileTime - ifModifiedSince;
                    if (diff <= TimeSpan.FromMilliseconds(0))
                    {
                        await Send304Async();
                        return;
                    }
                }
            }

            string contentType = null;
            switch (fi.Extension)
            {
                case ".html":
                case ".htm":
                    contentType = "text/html; charset=UTF-8";
                    break;
                case ".css":
                    contentType = "text/css; charset=UTF-8";
                    break;
                case ".js":
                    contentType = "application/javascript; charset=UTF-8";
                    break;
                case ".appcache":
                    contentType = "text/cache-manifest";
                    break;
                default:
                    contentType = MimeTypes.GetMimeType(fi.Extension);
                    break;
            }

            var dateTime = fi.LastWriteTime;
            var lastModified = dateTime.ToUniversalTime().ToString("r");

            try
            {
                using (Stream stream = File.OpenRead(file))
                    await SendStreamAsync(stream, contentType, lastModified, noCache);
            }
            catch (Exception e)
            {
                Logger.Current.Warning($"{Id} Could not send file: {e}");
            }
        }

        Task SendNotFoundAsync()
        {
            Logger.Current.LowTrace(() => $"{Id} 404 Not Found");
            return SendErrorAsync(
@"<title>CAESAR</title>
<Style> 
html {
    font-family: sans-serif;
}
h1 {
    font-weight: 100;
}
</Style>",
                "<h1>Datei nicht gefunden</h1><p>Die angegebene Resource konnte auf dem Server nicht gefunden werden.</p>",
                404, "Not Found");
        }


        Task UpgradeToWebSocketAsync()
        {
            var secKey = Headers["sec-websocket-key"];
            secKey += webSocketKeyConcat;
            var hashKey = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(secKey));
            var base64Key = Convert.ToBase64String(hashKey);
            var response = $"{HttpResponseString} 101 Switching Protocols\r\nConnection: Upgrade\r\nUpgrade: websocket\r\nSec-WebSocket-Accept: {base64Key}\r\n\r\n";
            if (Server.Configuration.HeaderTracing)
                Logger.Current.LowTrace(() => $"{Id} response");
            var bytes = Encoding.UTF8.GetBytes(response);
            return networkStream.WriteAsync(bytes, 0, bytes.Length);
        }

        IFormHandler CheckForm(string url)
            => Extension.FormHandlers
                .Where(n => url.StartsWith(n.Key, StringComparison.CurrentCultureIgnoreCase))
                .Select(n => n.Value)
                .FirstOrDefault();

        Isapi.Isapi CheckIsapi(string url)
            => Server.Configuration.Isapis.FirstOrDefault(n => !string.IsNullOrEmpty(url) && url.StartsWith(n?.Url, StringComparison.CurrentCultureIgnoreCase));

        bool CheckFile(string url, out string redirURL, out string localFile, out string pathInfo, out string queryString)
        {
            redirURL = "";
            localFile = "";
            pathInfo = "";
            queryString = "";

            var raute = url.IndexOf('#');
            if (raute != -1)
                url = url.Substring(0, raute);

            var qm = url.IndexOf('?');
            if (qm != -1)
            {
                queryString = url.Substring(qm + 1);
                url = url.Substring(0, qm);
            }

            if (!string.IsNullOrEmpty(TranslatedPathOverride))
            {
                localFile = TranslatedPathOverride;
                return File.Exists(TranslatedPathOverride);
            }

            var localURL = Uri.UnescapeDataString(url);

            var rootDirectory = Server.Configuration.Webroot;
            var relativePath = localURL.Replace('/', '\\');
            var alias = Server.Configuration.Aliases.FirstOrDefault(n => url.StartsWith(n.Value));
            if (alias != null)
            {
                relativePath = localURL.Substring(alias.Value.Length).Replace('/', '\\');

                if (alias.IsRooted)
                    rootDirectory = alias.Path;
                else
                    rootDirectory = Path.Combine(rootDirectory, alias.Path);
            }
            if (relativePath.StartsWith("\\"))
                relativePath = relativePath.Substring(1);

            try
            {
                localFile = Path.Combine(rootDirectory, relativePath);
            }
            catch (Exception e)
            {
                Logger.Current.Trace($"{Id} Invalid path: {Headers.Url}, {e}");
                throw new InvalidPathException();
            }

            // protect for directory traversal attacks
            if (localFile.Length < rootDirectory.Length || !localFile.StartsWith(rootDirectory))
            {
                Logger.Current.Warning($"{Id} POSSIBLE DIRECTORY TRAVERSAL ATTACK DETECTED! Url: {Headers.Url}");

                localFile = "";
                return false;
            }

            if (File.Exists(localFile))
                return true;

            if (Directory.Exists(localFile))
            {
                if (!url.EndsWith("/"))
                {
                    redirURL = url + '/' + (queryString.Length > 0 ? '?' + queryString : "");
                    return true;
                }
                else
                {
                    localFile = Path.Combine(localFile, alias?.DefaultFile ?? "index.html");
                    if (File.Exists(localFile))
                        return true;
                }
            }

            if (url == "/")
            {
                relativePath = "root\\index.html";
                var path = Path.Combine(Server.Configuration.Webroot, relativePath);
                if (File.Exists(path))
                {
                    redirURL = "/root/" + (queryString.Length > 0 ? '?' + queryString : "");
                    return true;
                }
            }

            var posLocalFile = localFile.LastIndexOf('\\');
            var posLocalURL = localURL.LastIndexOf('/');
            while (posLocalFile > 0 && posLocalURL > 0)
            {
                var fileStartPart = localFile.Substring(0, posLocalFile);
                if (File.Exists(fileStartPart))
                {
                    pathInfo = localURL.Substring(posLocalURL);
                    localFile = fileStartPart;
                    return true;
                }

                posLocalFile = localFile.LastIndexOf('\\', posLocalFile - 1);
                posLocalURL = localURL.LastIndexOf('/', posLocalURL - 1);
            }

            return false;
        }

        async Task<bool> SendInternalURLAsync(string url)
        {
            if (url == "/$$GC")
            {
                GC.Collect();
                await SendOKAsync("Speicher wurde bereinigt");
                return true;
            }
            else if (url == "/$$Resources")
            {
                await Resources.Current.SendAsync(this);
                return true;
            }

            return false;
        }

        async Task RedirectDirectoryAsync(string redirectedUrl)
        {
            if (!string.IsNullOrEmpty(Headers.Host))
            {
                var response = "<html><head>Moved permanently</head><body><h1>Moved permanently</h1>The specified resource moved permanently.</body></html>";
                var responseBytes = Encoding.UTF8.GetBytes(response);
                var redirectHeaders = $"{HttpResponseString} 301 Moved Permanently\r\nLocation: {UrlRoot}{redirectedUrl}\r\nContent-Length: {responseBytes.Length}\r\n\r\n";

                var headerBuffer = ASCIIEncoding.ASCII.GetBytes(redirectHeaders);
                await networkStream.WriteAsync(headerBuffer, 0, headerBuffer.Length);
                await networkStream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
        }

        string CreateXForwarded()
        {
            var https = Server.Configuration.IsTlsEnabled ? "S" : null;
            var port = Server.Configuration.IsTlsEnabled ?
                Server.Configuration.TlsPort != 443 ? Server.Configuration.TlsPort : 0 : Server.Configuration.Port != 80 ? Server.Configuration.Port : 0;
            var portstring = port != 0 ? $":{port}" : null;

            var url = Headers.Url;
            var pos = url.IndexOf('?');
            if (pos != -1)
            {
                pos = url.LastIndexOf('/', pos);
                if (pos != -1)
                    url = url.Substring(0, pos + 1);
            }
            return $"HTTP{https}://{Server.Configuration.DomainName}{portstring}{url}";
        }

        bool ServeOptions()
        {
            responseHeaders.Initialize(null, 0, null, false);
            var tcpPayload = responseHeaders.Access(SocketSession.UseTls, HttpResponseString, Headers);
            networkStream.Write(tcpPayload, 0, tcpPayload.Length);
            return true;
        }

        #endregion

        #region Structs

        struct ExtensionResult
        {
            public ExtensionResult(string url, Extension extension)
            {
                Url = url;
                Extension = extension;
            }
            readonly public string Url;
            readonly public Extension Extension;
        }

        #endregion

        #region Fields

        static int idSeed;

        const string webSocketKeyConcat = "60E914E4-18BE-4BC3-BC0B-C513D13E7021";
        ServerResponseHeaders responseHeaders;
        protected Stream networkStream;
        Stopwatch stopwatch;
        // TODO: muss kurzlebig sein!!
        byte[] readBuffer = new byte[80000]; // Muss unter 85kB sein, sonst landet der Buffer im LOH
        /// <summary>
        /// Die Position im Buffer, an der die Headerdaten aufhören und die eigentlichen Daten anfangen, unmittelbar nach Einlesen der Header aus dem Netzwerkstrom
        /// </summary>
        int bufferEndPosition;
        /// <summary>
        /// Wenn im Buffer nach dem Header bereits payload eingelesen wurde
        /// </summary>
        bool readFromBuffer;
        /// <summary>
        /// Anzahl bereits eingelesener Bytes im Buffer, unmittelbar nach Einlesen der Header aus dem Netzwerkstrom
        /// </summary>
        int bufferReadCount;
        bool isClosed;

        #endregion

        #region IDisposable Support

        bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    Instances.Decrement();
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}
