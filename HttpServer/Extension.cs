using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HttpServer.Enums;
using HttpServer.Exceptions;
using HttpServer.Interfaces;
using HttpServer.Sessions;
using HttpServer.WebService;
using HttpServer.WebSockets;

namespace HttpServer
{
    public class Extension
    {
        #region Properties

        public static Counter Instances { get; } = new Counter();

        public string[] Urls { get; private set; }

        public static Dictionary<string, IFormHandler> FormHandlers { get; set; } = new Dictionary<string, IFormHandler>();

        #endregion

        #region Constructor

        public Extension(IExtension extension, string module, string[] urls)
        {
            Urls = urls;
            this.module = module;
            Logger.Current.Info($"Async extension added, URL: {string.Join(", ", Urls)}");
            this.extension = extension;
        }

        public Extension(string module, string[] urls)
        {
            Urls = urls;
            this.module = module;
            Logger.Current.Info($"Async extension (delayed) added, URL: {string.Join(", ", Urls)}");
        }

        #endregion

        #region Method

        public async Task InitializeAsync(IServer server)
        {
            try
            {
                if (extension != null)
                {
                    try
                    {
                        Logger.Current.Info($"Initializing async extension {module}");
                        await extension.InitializeAsync(server);
                    }
                    catch (NotInitializedException nie)
                    {
                        Logger.Current.Info($"{nie.Message}: {nie.InnerException}");
                        initializationTimer = new Timer(async n =>
                        {
                            await semaphoreSlim.WaitAsync();
                            try
                            {
                                await extension.InitializeAsync(server);
                                initializationTimer.Dispose();
                            }
                            catch (NotInitializedException) { }
                            catch (Exception e)
                            {
                                Logger.Current.Fatal($"Extension {module} could not be initialized: {e}");
                                initializationTimer.Dispose();
                            }
                            finally
                            {
                                semaphoreSlim.Release();
                            }
                        }, null, nie.RetrySpan, nie.RetrySpan);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Current.Error($"Extension {module} could not be initialized: {e}");
            }
        }

        internal async Task<bool> RequestAsync(RequestSession session, Method method, string path, string query)
        {
            if (extension == null)
            {
                extension = ExtensionFactory.Current.OpenExtensionInterface(module);
                if (extension == null)
                    throw new Exception($"Could not load async extension: {module}");
                await InitializeAsync(session.Server);
            }

            Service service = null;
            try
            {
                service = await session.GetServiceInfoAsync() as Service;
                Instances.Increment();

                if (session.CheckWsUpgrade())
                {
                    if (extension is IWebSocketsConsumer webSocketsConsumer)
                    {
                        var webSocketSession = await session.UpgradeWebSocketAsync();
                        var webSocket = new WebSocket(webSocketSession);
                        webSocketsConsumer.OnNew(webSocket, query);
                    }
                    Instances.DecrementActive();
                    Instances.Decrement();
                    return false;
                }

                if (service != null)
                    await extension.ServiceRequestAsync(service);
                else
                {
                    var urlQuery = new UrlQueryComponents(query);
                    await extension.RequestAsync(session, method, path, urlQuery);
                }
                return true;
            }
            catch (Exception e)
            {
                try
                {
                    return await ProcessExceptionAsync(e, session, extension, service);
                }
                catch (Exception ee)
                {
                    Logger.Current.Info($"{session.Id} Async socket session closed, an error has occurred: {ee}");
                    try
                    {
                        await session.SendExceptionAsync(ee);
                    }
                    catch { }
                    session.Close(true);
                    return false;
                }
            }
            finally
            {
                Instances.DecrementActive();
                Instances.Decrement();
            }
        }

        public Task ShutdownAsync() => extension?.ShutdownAsync() ?? Task.FromResult(0);

        async Task<bool> ProcessExceptionAsync(Exception e, ISession session, IExtension extension, IService service)
        {
            switch (e)
            {
                case TargetInvocationException tie:
                    var exception = tie.GetBaseException();
                    if (!await extension.OnErrorAsync(exception, service))
                        throw e;
                    return true;
                default:
                    if (service == null)
                    {
                        Logger.Current.Error($"Internal server error in Extension.Request. {e}");
                        await session.SendExceptionAsync(e);
                        session.Close();
                        return false;
                    }
                    else if (!await extension.OnErrorAsync(e, service))
                        throw e;
                    return true;
            }
        }
        #endregion

        #region Fields

        readonly static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        readonly string module;
        IExtension extension;
        Timer initializationTimer;

        #endregion
    }
}
