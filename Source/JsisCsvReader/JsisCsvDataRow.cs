using Gemstone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsisCsvReader
{
    public class JsisCsvDataRow
    {
        public Ticks Timestamp { get; internal set; }
        public bool AllowQueuedPublication { get; internal set; }
    }
}
