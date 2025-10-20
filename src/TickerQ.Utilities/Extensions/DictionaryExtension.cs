using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickerQ.Utilities.Extensions
{
    public static class DictionaryExtension
    {
        public static Dictionary<string, string> Clone(this Dictionary<string, string> source)
        {
            var dict = new Dictionary<string, string>();
            foreach (var kvp in source)
            {
                dict[kvp.Key] = kvp.Value;
            }
            return dict;
        }
    }
}
