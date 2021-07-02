using Adapt.Models;
using GemstoneCommon;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Adapt.DataSources
{
    /// <summary>
    /// Represents a data source adapter that imports data from JSIS CSV Files.
    /// </summary>
    [Description("JSIS-CSV Import: Imports data from a set of JSIS-CSV files.")]
    public class JsisCsvImport : IDataSource
    {

        #region [ Members ]
        private JsisCsvSettings m_settings;
        private Dictionary<DateTime, string> m_Files = new Dictionary<DateTime, string>();
        private ManualResetEvent m_FileComplete;
        #endregion

        #region [ Constructor ]
        public JsisCsvImport()
        {
        }
        #endregion
        #region [ Methods ]
        public void Configure(IConfiguration config)
        {
            m_settings = new JsisCsvSettings();
            config.Bind(m_settings);

            m_Files = new Dictionary<DateTime, string>();

            // Find all Files and parse into dateTime
            if (!Directory.Exists(m_settings.RootFolder))
                return;

            List<string> files = Directory.GetFiles(m_settings.RootFolder, "*.csv", SearchOption.AllDirectories).ToList();

            //Parse these Files into DateTime and File Path
            foreach (string path in files)
            {
                FileInfo fileInfo = new FileInfo(path);
                MatchCollection matches = m_DateTimeParsing.Matches(fileInfo.Name);

                if (matches.Count == 0)
                    continue;

                string date = matches[0].Groups[1].ToString();
                string time = matches[0].Groups[2].ToString();
                DateTime dateTime;

                try
                {
                    dateTime = DateTime.ParseExact((date + "-" + time), "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    continue;
                }

                if (m_Files.ContainsKey(dateTime))
                    m_Files[dateTime] = fileInfo.FullName;
                else
                    m_Files.Add(dateTime, fileInfo.FullName);
            }

            //GetConfigFrame();
        }

        public IAsyncEnumerable<IFrame> GetData(List<AdaptSignal> signals, DateTime start, DateTime end)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<AdaptDevice> GetDevices()
        {
            throw new NotImplementedException();
        }

        public double GetProgress()
        {
            throw new NotImplementedException();
        }

        public Type GetSettingType()
        {
            return typeof(JsisCsvSettings);
        }

        public IEnumerable<AdaptSignal> GetSignals()
        {
            return new List<AdaptSignal>();
        }

        public bool SupportProgress()
        {
            throw new NotImplementedException();
        }

        public bool Test()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region [ static ]
        private static Regex m_DateTimeParsing = new Regex(@"_([0-9]{8})_([0-9]{6})\.csv$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        #endregion
    }
}
