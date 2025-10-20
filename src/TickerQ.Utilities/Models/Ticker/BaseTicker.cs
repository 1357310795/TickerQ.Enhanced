using System;
using System.Collections.Generic;

namespace TickerQ.Utilities.Models.Ticker
{
    public class BaseTicker
    {
        public Guid Id { get; set; }
        public string Function { get; set; }
        public string Description { get; set; }
        public string InitIdentifier { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsPaused { get; set; }
        public Dictionary<string, string> DataMap { get; set; }
    }
}
