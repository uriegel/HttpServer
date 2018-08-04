using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpServer
{
    class ThreadTask<T>
    {
        public static Task<T> RunAsync(Func<T> function)
        {
            var tcs = new TaskCompletionSource<T>();
            (new Thread(() =>
            {
                try
                {
                    var result = function();
                    tcs.SetResult(result);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            })
            {
                IsBackground = true
            }).Start();
            return tcs.Task;
        }
    }
}