using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Exceptions
{
    public class NotInitializedException : Exception
    {
        public TimeSpan RetrySpan { get; }

        internal NotInitializedException(string message, Exception e, TimeSpan retrySpan) : base(message, e)
            => RetrySpan = retrySpan;
    }
}
