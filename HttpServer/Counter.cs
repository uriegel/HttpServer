using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpServer
{
    public class Counter
    {
        public int Count { get => count; } 
        int count;

        public int ActiveCount { get => activeCount; } 
        int activeCount;

        public int TotalCount { get => totalCount; } 
        int totalCount;

        public void Increment()
        {
            Interlocked.Increment(ref count);
            Interlocked.Increment(ref activeCount);
            Interlocked.Increment(ref totalCount);
        }

        public void DecrementActive() => Interlocked.Decrement(ref activeCount);
        public void Decrement() => Interlocked.Decrement(ref count);
    }
}
