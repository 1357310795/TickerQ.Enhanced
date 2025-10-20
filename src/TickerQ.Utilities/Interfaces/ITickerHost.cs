using System;
using System.Threading;
using System.Threading.Tasks;
using TickerQ.Utilities.Models;

namespace TickerQ.Utilities.Interfaces
{
    public interface ITickerHost
    {
        void Start();
        void RestartIfNeeded(DateTime newOccurrence);
        void Restart();
        void RestartThrottled();
        void Stop();
        bool IsRunning();
        abstract Task ExecuteTaskAsync(InternalFunctionContext context,
            TickerFunctionDelegate delegateFunction,
            bool isDue,
            CancellationToken cancellationToken = default);
        DateTime? NextPlannedOccurrence { get; }
    }
}
