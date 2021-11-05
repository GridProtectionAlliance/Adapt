using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsisCsvReader
{
    public class JsisCsvData
    {

        #region [ Properties ]
        public string Filename { get; set; }
        public string PMUName { get; set; }
        public JsisCsvHeader Header { get; set; }
        public List<JsisCsvDataRow> Data { get; set; }
        public DateTime BaseTime { get; set; }
        #endregion
    }
}
