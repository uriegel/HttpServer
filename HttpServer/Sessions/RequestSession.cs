using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HttpServer.Exceptions;
using HttpServer.Headers;
using HttpServer.Interfaces;
using HttpServer.WebSockets;

namespace HttpServer.Sessions
{
    class RequestSession : ISession, IDisposable
    {
        #region Properties

        public string Id { get; }
        public static Counter Instances { get; } = new Counter();
        public Server Server { get; }
        public SocketSession SocketSession { get; }
        public IPEndPoint LocalEndPoint { get; private set; }
        public IPEndPoint RemoteEndPoint { get; private set; }

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
        public async Task<bool> StartAsync()
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


        public Service GetServiceInfo()
            => throw new NotImplementedException();

        public bool CheckWsUpgrade() => throw new NotImplementedException();

        public Task<WebSocketSession> UpgradeWebSocketAsync() => throw new NotImplementedException();
         
        public Task SendExceptionAsync(Exception e) => throw new NotImplementedException();

        public void Close() => Close(false);

        public void Close(bool fullClose)
        {
            try
            {
                if (fullClose)
                {
                    //networkStream.Close();
                    //isClosed = true;
                }
                //else
                    // SocketSession.Client.Client.Shutdown(SocketShutdown.Send);
            }
            catch { }
        }

        public async Task SendHtmlStringAsync(string html)
        {
            var bytes = Encoding.UTF8.GetBytes(html);
            //responseHeaders.Add("Content-Length", $"{bytes.Length}");
            //responseHeaders.Add("Content-Type", "text/html; charset=UTF-8");
            //var headerBuffer = responseHeaders.Access(SocketSession.UseTls, HttpResponseString, Headers);
            //await WriteAsync(headerBuffer, 0, headerBuffer.Length);
            //await WriteAsync(bytes, 0, bytes.Length);
        }

        async Task<bool> ReceiveAsync(int bufferPosition)
        {
            try
            {
                //Headers = new RequestHeaders();
                //var result = await (Headers as Headers).InitializeAsync(networkStream, readBuffer, bufferPosition, Server.Configuration.HeaderTracing, Id);
                //bufferEndPosition = result.BufferEndPosition;
                //readFromBuffer = bufferEndPosition > 0;
                //bufferReadCount = result.BufferReadCount;

                //Logger.Current.Trace($"{Id} Request: {RemoteEndPoint} \"{Headers.Method} {Headers.Url} {Headers.Http}\"");

                //stopwatch?.Start();

                ////string refererPath = null;
                ////if (headers.ContainsKey("Referer"))
                ////{
                ////    string path = headers["Referer"];
                ////    path = path.Substring(path.IndexOf("//") + 2);
                ////    refererPath = path.Substring(path.IndexOf('/') + 1);
                ////}

                //if (Headers.Method == Web.Method.OPTIONS)
                //    return ServeOptions();

                //string query = null;
                //var asyncExtensionResult = CheckAsyncExtension(Headers.Url);
                //if (asyncExtensionResult.HasValue)
                //{
                //    var path = asyncExtensionResult.Value.Url;
                //    query = Headers.Url.Substring(path.Length).Trim('/');
                //    return await asyncExtensionResult.Value.Extension.RequestAsync(this, Headers.Method, path, query) && !isClosed;
                //}

                //var extensionResult = CheckExtension(Headers.Url);
                //if (extensionResult != null)
                //    return await ThreadTask<bool>.RunAsync(() =>
                //    {
                //        var path = extensionResult.Value.Url;
                //        query = Headers.Url.Substring(path.Length).Trim('/');
                //        return extensionResult.Value.Extension.Request(this, Headers.Method, path, query) && !isClosed;
                //    });

                //var isapi = CheckIsapi(Headers.Url);
                //if (isapi != null)
                //    if (await ThreadTask<bool>.RunAsync(() =>
                //    {
                //        var request = isapi.CreateRequest();
                //        string path;
                //        var pos = Headers.Url.IndexOf('?');
                //        if (pos != -1)
                //        {
                //            query = Headers.Url.Substring(pos + 1);
                //            path = Headers.Url.Substring(0, pos);
                //        }
                //        else
                //            path = Headers.Url;

                //        if (isapi.Url.Length == path.Length)
                //            path = null;
                //        else if (isapi.Url == "/" && (path?.StartsWith("/") ?? false))
                //            path = path.Substring(1);

                //        else if (isapi.Url.Length > 1 && isapi.Url.Length < path.Length)
                //            path = path.Substring(isapi.Url.Length + 1);

                //        if (!string.IsNullOrEmpty(path))
                //            path = Uri.UnescapeDataString(path);

                //        lock (locker)
                //        {
                //            if (request.Request(this, Headers.Method, Headers.ContentType, TranslatedPathOverride, path, query))
                //            {
                //                request.SetInfo(responseHeaders);
                //                return true;
                //            }
                //        }
                //        return false;
                //    }))
                //        return true;

                //var proxyRedirection = Server.Configuration.CheckProxyRedirection(Headers.Url);
                //if (proxyRedirection != null)
                //    return await RedirectAsync(proxyRedirection.ExtractOriginalUrl(Headers.Url), true);

                //var redirection = CheckRedirection(Headers.Url);
                //if (redirection != null)
                //{
                //    if (redirection.IsProxy())
                //        return await RedirectAsync(redirection.GetRedirectedUrl(Headers.Url));
                //    else
                //    {
                //        await RedirectDirectoryAsync(redirection.GetRedirectedUrl(Headers.Url));
                //        return true;
                //    }
                //}

                //if (Headers.Method == Web.Method.POST && Headers.ContentType == "application/x-www-form-urlencoded" ||
                //    Headers.Method == Web.Method.GET && Headers.Url.Contains('?'))
                //{
                //    var formHandler = CheckForm(Headers.Url);
                //    if (formHandler != null)
                //    {
                //        var queryComponents = new Web.UrlQueryComponents(Headers.Method == Web.Method.POST ? $"{Headers.Url}?{ReadString()}" : Headers.Url);
                //        await SendHtmlStringAsync(await formHandler.OnSubmitAsync(this, Headers.Method, Headers.Url, queryComponents));
                //        return true;
                //    }
                //}

                //if (SendInternalURL(Headers.Url))
                //    return true;

                //if (!CheckFile(Headers.Url, out string redirURL, out string localFile, out string pathInfo, out string queryString))
                //{
                //    await SendNotFoundAsync();
                //    return true;
                //}

                //if (!String.IsNullOrEmpty(redirURL))
                //{
                //    await RedirectDirectoryAsync(redirURL);
                //    return true;
                //}

                //var isapiScript = Server.Configuration.IsapiScripts?.FirstOrDefault(n => (localFile.EndsWith(n.Extension, StringComparison.OrdinalIgnoreCase)));
                //if (isapiScript != null && await ThreadTask<bool>.RunAsync(() => SendIsapiScript(isapiScript, localFile, pathInfo, queryString)))
                //    return true;

                //var fastCGIScript = Server.Configuration.FastCGIScripts?.FirstOrDefault(n => (localFile.EndsWith(n.Extension, StringComparison.OrdinalIgnoreCase)));
                //if (fastCGIScript != null && await ThreadTask<bool>.RunAsync(() => SendFastCGIScript(fastCGIScript, localFile, pathInfo, queryString)))
                //    return true;

                //await SendFileAsync(localFile);
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
//                Logger.Current.Trace($"{Id} Answer: {RemoteEndPoint} \"{Headers.Method} {Headers.Url} {Headers.Http}\" Status: {responseHeaders.Status} Size: {responseHeaders.ContentLength} Duration: {elapsed}");
            }
        }

        #endregion

        #region Fields

        static int idSeed;

        ServerResponseHeaders responseHeaders;
        Stream networkStream;
        Stopwatch stopwatch;
        // TODO: muss kurzlebig sein!!
        byte[] readBuffer = new byte[80000]; // Muss unter 85kB sein, sonst landet der Buffer im LOH

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
