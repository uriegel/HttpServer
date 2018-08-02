using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer.Enums;

namespace HttpServer.Interfaces
{
    public interface IFormHandler
    {
        /// <summary>
        /// Abarbeitung eines Formulars
        /// </summary>
        /// <param name="session">Die RequestSession</param>
        /// <param name="method">HTML-Methode (POST oder GET)</param>
        /// <param name="path">Der Pfad zur gewünschten Ergebnis-HTML</param>
        /// <param name="urlQuery">Query-Komponenten, welche die Formular-Parameter enthalten</param>
        /// <returns>Die Ergebnis-HTML-Seite</returns>
        Task<string> OnSubmitAsync(ISession session, Method method, string path, Web.UrlQueryComponents urlQuery);
    }
}
