using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HttpServer
{
    /// <summary>
    /// Hilfsroutinen für die Untersuchung und Zerlegung von WEB-Urls
    /// </summary>
    static public class UrlUtility
    {
        #region Methods

        /// <summary>
        /// Zerlegung einer Url in einzelne Url-Teile und Parametern, die sich mit <see cref="GetParameters"/> weiter zerlegen lassen
        /// </summary>
        /// <param name="url">Die zu untersuchende Url</param>
        /// <returns>Die in die Bestandteile zerlegete Url</returns>
        public static UrlParts GetUrlParts(string url)
        {
            var urlParts = new UrlParts();
            if (string.IsNullOrEmpty(url))
                return urlParts;
            int firstSlash;
            var doubleSlash = url.IndexOf("//");
            if (doubleSlash != -1)
                firstSlash = url.IndexOf("/", doubleSlash + 2);
            else
                firstSlash = url.IndexOf("/");
            if (firstSlash == -1)
                urlParts.ServerPart = url;
            else
            {
                urlParts.ServerPart = url.Substring(0, firstSlash);
                urlParts.ServerIndependant = url.Substring(firstSlash);
            }

            var match = urlPartsRegex.Match(url);
            if (!match.Success)
                return urlParts;

            if (firstSlash != -1 && url.Length > firstSlash + 1)
                urlParts.ResourceParts = match.Groups["ResourcePart"].Captures.OfType<Capture>().Select(n => n.Value).ToArray();
            urlParts.Parameters = match.Groups["Parameters"].Value;
            return urlParts;
        }

        /// <summary>
        /// Zerlegung des Parameters-Teils einer URL
        /// </summary>
        /// <param name="urlParameterString">Der zu untersuchende Parameter-Teil einer Url</param>
        /// <returns>Die Parameter als Array von Key-Value-Pairs (Parametername, Parameterwert)</returns>
        public static KeyValuePair<string, string>[] GetParameters(string urlParameterString)
        {
            var mc = urlParameterRegex.Matches(urlParameterString);
            return mc.OfType<Match>().Select(n => new KeyValuePair<string, string>(n.Groups["key"].Value,
                Uri.UnescapeDataString(UnescapeSpaces(n.Groups["value"].Value)))).ToArray();
        }

        /// <summary>
        /// Aus Parametern die in der URL übergeben worden sind ein JSON-String erzeugen, welches sich in ein C#-Objekt umwandeln lassen kann
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string GetJsonFromUrlParameters(IEnumerable<KeyValuePair<string, string>> parameters)
            => $"{{{string.Join(",", parameters.Select(n => string.Format(@"""{0}"":""{1}""", n.Key, n.Value)))}}}";

        static string UnescapeSpaces(string uri) => uri.Replace('+', ' ');

        #endregion

        #region Fields

        static Regex urlPartsRegex = new Regex(@"(http://[^/]+)?(?:/(?<ResourcePart>[^<>?&/#\""]+))+(?:\?(?<Parameters>.+))?", RegexOptions.Compiled);
        static Regex urlParameterRegex = new Regex(@"(?<key>[^&?]*?)=(?<value>[^&?]*)", RegexOptions.Compiled);

        #endregion
    }
}
