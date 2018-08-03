using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Extensions
{
    public static class TaskExtensions
    {
        public static T Synchronize<T>(this Task<T> task)
        {
            try
            {
                return task.Result;
            }
            catch (AggregateException ae)
            {
                throw ae.InnerException;
            }
        }

        public static T Synchronize<T>(this Task<T> task, TimeSpan timeout)
        {
            try
            {
                var ret = Task.WaitAny(new[] { task, Task.Delay(timeout) });
                if (ret == 0)
                {
                    if (task.Status != TaskStatus.Faulted)
                        return task.Result;
                    else
                        throw task.Exception;
                }
                else
                    throw new TimeoutException();
            }
            catch (AggregateException ae)
            {
                throw ae.InnerException;
            }
        }

        public static void Synchronize(this Task task, TimeSpan? timeout = null)
        {
            try
            {
                if (timeout.HasValue)
                    task.Wait(timeout.Value);
                else
                    task.Wait();
            }
            catch (AggregateException ae)
            {
                throw ae.InnerException;
            }
        }
    }
}
