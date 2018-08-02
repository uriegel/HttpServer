using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer
{
    public struct UrlQueryComponents
    {
        public string Path;
        public Dictionary<string, string> Parameters;

        public UrlQueryComponents(string query)
        {
            Path = null;

            if (!string.IsNullOrEmpty(query) && query.Contains('?'))
            {
                var pos = query.IndexOf('?');
                if (pos >= 0)
                {
                    Path = query.Substring(0, pos);
                    Parameters = UrlUtility.GetParameters(query).ToDictionary(n => n.Key, n => n.Value);
                }
                else
                {
                    Path = query;
                    Parameters = new Dictionary<string, string>();
                }
            }
            else
            {
                Path = query;
                Parameters = new Dictionary<string, string>();
            }
        }
    }
}
