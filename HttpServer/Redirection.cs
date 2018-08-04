using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer
{
    public class Redirection
    {
        #region Properties	

        public string RedirectionBaseUrl { get; private set; }
        #endregion

        #region Methods	

        public string GetRedirectedUrl(string url)
        {
            string additional = null;
            if (url.Length > RedirectionBaseUrl.Length)
                additional = url.Substring(RedirectionBaseUrl.Length);
            return target + additional;
        }

        public bool IsProxy() => proxy;

        #endregion

        #region Constructor	

        public Redirection(string redirectionBaseUrl, string target, bool proxy)
        {
            RedirectionBaseUrl = redirectionBaseUrl;
            this.target = target;
            this.proxy = proxy;
        }

        #endregion

        #region Fields	

        string target;
        bool proxy;

        #endregion
    }
}
