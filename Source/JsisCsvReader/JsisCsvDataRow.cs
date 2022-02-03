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
        private string m_device;

        public JsisCsvDataRow()
        {
            PhasorDefinitions = new List<JsisCsvChannel>();
            AnalogDefinitions = new List<JsisCsvChannel>();
            DigitalDefinitions = new List<JsisCsvChannel>();
            FrequencyDefinition = new List<JsisCsvChannel>();
            CustomDefinitions = new List<JsisCsvChannel>();
        }
        public Ticks Timestamp { get; internal set; }
        public bool AllowQueuedPublication { get; internal set; }
        public List<JsisCsvChannel> PhasorDefinitions { get; set; }
        public List<JsisCsvChannel> AnalogDefinitions { get; set; }
        public List<JsisCsvChannel> DigitalDefinitions { get; set; }
        public List<JsisCsvChannel> FrequencyDefinition { get; set; }
        public List<JsisCsvChannel> CustomDefinitions { get; set; }
        public string PMUName => m_device;
    }
}
