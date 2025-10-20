using System;
using System.Collections.Generic;
using TickerQ.Utilities.Enums;

namespace TickerQ.Utilities.Models
{
    public class InternalFunctionContext
    {
        public string FunctionName { get; set; }
        public Guid TickerId { get; set; }
        public Guid OccurrenceId { get; set; }
        public TickerType Type { get; set; }
        public int Retries { get; set; }
        public int RetryCount { get; set; }
        public TickerStatus Status { get; set; }
        public long ElapsedTime { get; set; }
        public string ExceptionDetails { get; set; }
        public int[] RetryIntervals { get; set; }
        public object Result { get; set; }
        public Dictionary<string, string> DataMap { get; set; }
    }
}
