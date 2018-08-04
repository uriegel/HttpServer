using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace HttpServer.Extensions
{
    static class HttpWebRequestExtensions
    {
        public static void CertificateValidator(this HttpWebRequest webRequest, Func<Exception, bool> check)
        {
            webRequest.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.RemoteCertificateChainErrors)
                {
                    var chainDescriptions = chain.ChainStatus != null ? chain.ChainStatus.Select(n => $"Chain certificate error: {n.Status} {n.StatusInformation}").ToArray() : null;
                    var e = new Exception(CertificateError.Chain, "Remote certificate chain error", chainDescriptions);
                    return check(e);
                }
                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) == SslPolicyErrors.RemoteCertificateNotAvailable)
                {
                    var e = new Exception(CertificateError.RemoteCertificateNotAvailable, "Remote certificate not available");
                    return check(e);
                }
                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch)
                {
                    var alternativeNamesExtension = (certificate as X509Certificate2)?.Extensions.AsEnumerable().FirstOrDefault(n => n.Oid.Value == "2.5.29.17"); // SAN OID
                    if (alternativeNamesExtension != null)
                    {
                        var alts = alternativeNamesExtension.Format(false).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                        var result = alts.Any(n => n.EndsWith(webRequest.RequestUri.Host, StringComparison.InvariantCultureIgnoreCase));
                        if (result)
                            return true;
                        else
                        {
                            var e = new Exception(CertificateError.NameMismatch, "Remote certificate name mismatch");
                            return check(e);
                        }
                    }
                }
                return true;
            };
        }

        public enum CertificateError
        {
            Chain,
            RemoteCertificateNotAvailable,
            NameMismatch
        }

        public class Exception : System.Exception
        {
            public Exception(CertificateError error, string message) : base(message)
            {
                Error = error;
            }

            public Exception(CertificateError error, string message, string[] chainErrorDescriptions) : base(message)
            {
                Error = error;
                ChainErrorDescriptions = chainErrorDescriptions;
            }

            public readonly CertificateError Error;
            public readonly string[] ChainErrorDescriptions;
        }
    }
}
