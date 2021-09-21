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
            //in this getconfigframe function, the jsiscsv reader might be called?

        }

        public async IAsyncEnumerable<IFrame> GetData(List<AdaptSignal> signals, DateTime start, DateTime end)
        {
            //Create List of Relevant Files
            List<DateTime> files = m_Files.Keys.Where(k => k < end && k > start).ToList();

            if (m_Files.Keys.Where(k => k <= start).Count() > 0)
            {
                DateTime initial = start - m_Files.Keys.Where(k => k <= start).Min(k => (start - k));

                files.Add(initial);
            }

            if (files.Count() == 0)
                yield break;

            // this is where actual signal data is read from csv files.
        }

        public IEnumerable<AdaptDevice> GetDevices()
        {
            return new List<AdaptDevice>();
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
            return false;
        }

        public bool Test()
        {
            if (!Directory.Exists(m_settings.RootFolder))
                return false;

            return m_Files.Count > 0;
        }
        #endregion

        #region [ static ]
        private static Regex m_DateTimeParsing = new Regex(@"_([0-9]{8})_([0-9]{6})\.csv$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        #endregion
    }
}
