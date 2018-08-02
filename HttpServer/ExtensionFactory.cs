using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HttpServer.Interfaces;

namespace HttpServer
{
    public class ExtensionFactory
    {
        #region Properties	

        public static ExtensionFactory Current { get; } = new ExtensionFactory();

        #endregion

        #region Methods	

        public Extension Create(string module, string[] urls, bool loadInitially)
        {
            try
            {
                if (loadInitially)
                    return new Extension(OpenExtensionInterface(module), module, urls);
                else
                    return new Extension(module, urls);
            }
            catch (Exception e)
            {
                Logger.Current.Error($"Could not load async extension: {module}, error: {e}");
                return null;
            }
        }

        public IExtension OpenExtensionInterface(string module)
        {
            try
            {
                var type = Type.GetType($"{ module}.RequestInstance,{module}");
                if (type == null)
                    return null;
                var assi = Assembly.GetAssembly(type);
                return assi.CreateInstance(type.FullName) as IExtension;
            }
            catch (Exception e)
            {
                Logger.Current.LowTrace(() => $"Could not open async extension interface: {module}, error: {e}");
                return null;
            }
        }

        #endregion

        #region Constructor	

        ExtensionFactory()
        {
        }

        #endregion
    }
}
