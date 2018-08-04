using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace HttpServer.Extensions
{
    public static class X509ExtensionCollectionExtensions
    {
        public static IEnumerable<X509Extension> AsEnumerable(this X509ExtensionCollection x509Collextion)
        {

            if (x509Collextion == null)
                yield break;
            for (var i = 0; i < x509Collextion.Count; i++)
                yield return x509Collextion[i];
        }
    }
}
