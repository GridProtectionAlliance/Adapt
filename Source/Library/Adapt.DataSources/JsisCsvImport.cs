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
using JsisCsvReader;
using Gemstone;
using GemstonePhasorProtocolls;
using System.Threading.Channels;
using System.Collections.Concurrent;

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
        private Channel<JsisCsvDataRow> m_dataQueue;
        private TaskCompletionSource<JsisCsvHeader> m_jsisCsvFileSource;
        #endregion

        #region [ Constructor ]
        public JsisCsvImport()
        {
            m_jsisCsvFileSource = new TaskCompletionSource<JsisCsvHeader>();
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

            GetJsisCsvHeader();
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

            //Start File Read Process
            m_dataQueue = Channel.CreateUnbounded<JsisCsvDataRow>();

            // this is where actual signal data is read from csv files.
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            ReadFile(files, tokenSource.Token).Start();


            await foreach (JsisCsvDataRow frame in m_dataQueue.Reader.ReadAllAsync())
            {
                if (frame.Timestamp < new Ticks(start))
                    continue;
                if (frame.Timestamp > new Ticks(end))
                {
                    tokenSource.Cancel();
                    continue;
                }

                IEnumerable<ITimeSeriesValue> magnitudes = frame.PhasorDefinitions.Select(item => new Tuple<int, JsisCsvChannel>(signals.FindIndex(s => s.Device == item.Device && s.ID == item.Name), item))
                    .Where(item => item.Item1 != -1)
                    .Select(item => new AdaptValue(signals[item.Item1].ID)
                    {
                        Value = item.Item2.Measurement,
                        Timestamp = frame.Timestamp,
                    });

                //IEnumerable<ITimeSeriesValue> phases = frame.Cells.SelectMany(item => item.PhasorValues)
                //    .Select(item => new Tuple<int, IPhasorValue>(signals.FindIndex(s => s.Device == item.Parent.IDCode.ToString() && s.ID == item.Label + "-Ph"), item))
                //    .Where(item => item.Item1 != -1)
                //    .Select(item => new AdaptValue(signals[item.Item1].ID)
                //    {
                //        Value = item.Item2.Angle.ToDegrees(),
                //        Timestamp = frame.Timestamp,
                //    });

                IEnumerable<ITimeSeriesValue> analogs = frame.AnalogDefinitions.Select(item => new Tuple<int, JsisCsvChannel>(signals.FindIndex(s => s.Device == item.Device && s.ID == item.Name), item))
                    .Where(item => item.Item1 != -1)
                    .Select(item => new AdaptValue(signals[item.Item1].ID)
                    {
                        Value = item.Item2.Measurement,
                        Timestamp = frame.Timestamp,
                    });

                IEnumerable<ITimeSeriesValue> frequencies = frame.FrequencyDefinition.Select(item => new Tuple<int, JsisCsvChannel>(signals.FindIndex(s => s.Device == item.Device && s.ID == item.Name), item))
                    .Where(item => item.Item1 != -1)
                    .Select(item => new AdaptValue(signals[item.Item1].ID)
                    {
                        Value = item.Item2.Measurement,
                        Timestamp = frame.Timestamp,
                    }); ;

                IEnumerable<ITimeSeriesValue> digitals = frame.DigitalDefinitions.Select(item => new Tuple<int, JsisCsvChannel>(signals.FindIndex(s => s.Device == item.Device && s.ID == item.Name), item))
                    .Where(item => item.Item1 != -1)
                    .Select(item => new AdaptValue(signals[item.Item1].ID)
                    {
                        Value = item.Item2.Measurement,
                        Timestamp = frame.Timestamp,
                    });

                IFrame outFrame = new Frame()
                {
                    Published = true,
                    Timestamp = frame.Timestamp,
                    Measurements = new ConcurrentDictionary<string, ITimeSeriesValue>(magnitudes.Concat(analogs).Concat(digitals).Concat(frequencies).Select(item => new KeyValuePair<string, ITimeSeriesValue>(item.ID, item)))
                };

                yield return outFrame;

            }


            tokenSource.Cancel();
        }

        public Task ReadFile(List<DateTime> files, CancellationToken cancellationToken)
        {
            return new Task(() => {

                if (m_FileComplete != null)
                    m_FileComplete.Set();

                for (int i = 0; i < files.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    JsisCsvParser parser = new JsisCsvParser();
                    m_FileComplete = new ManualResetEvent(false);
                    //parser.PhasorProtocol = PhasorProtocol.Jsis_CSV;
                    parser.TransportProtocol = Gemstone.Communication.TransportProtocol.File;
                    parser.ConnectionString = $"file={m_Files[files[i]]}";
                    parser.ConnectionTerminated += ConnectionTerminated;
                    parser.ReceivedDataFrame += RecievedDataFrame;
                    parser.DefinedFrameRate = 10000000;
                    parser.Start();
                    WaitHandle.WaitAny(new WaitHandle[] { m_FileComplete, cancellationToken.WaitHandle });
                    parser.Stop();
                }

                m_dataQueue.Writer.Complete();
            }, cancellationToken);
        }

        /// <summary>
        /// Get list of PMUs
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AdaptDevice> GetDevices()
        {
            var devices = new List<AdaptDevice>();
            if (!m_jsisCsvFileSource.Task.IsCompleted && m_jsisCsvFileSource.Task.Status != TaskStatus.WaitingForActivation)
                return devices;

            JsisCsvHeader configuration = (JsisCsvHeader)m_jsisCsvFileSource.Task.Result;
            devices.Add(new AdaptDevice(configuration.PMUName));

            return devices;
        }

        public double GetProgress()
        {
            throw new NotImplementedException();
        }

        public Type GetSettingType()
        {
            return typeof(JsisCsvSettings);
        }
        /// <summary>
        /// Get list of Signals of each PMU
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AdaptSignal> GetSignals()
        {
            if (!m_jsisCsvFileSource.Task.IsCompleted && m_jsisCsvFileSource.Task.Status != TaskStatus.WaitingForActivation)
                return new List<AdaptSignal>();
            JsisCsvHeader configuration = (JsisCsvHeader)m_jsisCsvFileSource.Task.Result;
            IEnumerable<AdaptSignal> analogs = configuration.AnalogDefinitions.Select(aD => new AdaptSignal(aD.Name, aD.Name, aD.Device)
            {
                FramesPerSecond = aD.FramesPerSecond,
                Phase = aD.Phase,
                Type = aD.Type
            });
            IEnumerable<AdaptSignal> digitals = configuration.DigitalDefinitions.Select(aD => new AdaptSignal(aD.Name, aD.Name, aD.Device)
            {
                FramesPerSecond = aD.FramesPerSecond,
                Phase = aD.Phase,
                Type = aD.Type
            });
            IEnumerable<AdaptSignal> phases = configuration.PhasorDefinitions.Select(aD => new AdaptSignal(aD.Name, aD.Name, aD.Device)
            {
                FramesPerSecond = aD.FramesPerSecond,
                Phase = aD.Phase,
                Type = aD.Type
            });
            IEnumerable<AdaptSignal> frequency = configuration.FrequencyDefinition.Select(aD => new AdaptSignal(aD.Name, aD.Name, aD.Device)
            {
                FramesPerSecond = aD.FramesPerSecond,
                Phase = aD.Phase,
                Type = aD.Type
            });
            IEnumerable<AdaptSignal> custom = configuration.CustomDefinitions.Select(aD => new AdaptSignal(aD.Name, aD.Name, aD.Device)
            {
                FramesPerSecond = aD.FramesPerSecond,
                Phase = aD.Phase,
                Type = aD.Type
            });
            return phases.Concat(digitals).Concat(analogs).Concat(frequency).Concat(frequency).Concat(custom);
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

        private void GetJsisCsvHeader()
        {
            if (m_Files.Count == 0)
                return;

            if (m_jsisCsvFileSource.Task.IsCompleted)
                return;

            JsisCsvParser parser = new JsisCsvParser();
            //parser.PhasorProtocol = PhasorProtocol.Jsis_CSV;
            parser.TransportProtocol = Gemstone.Communication.TransportProtocol.File;
            parser.ConnectionString = $"file={m_Files.First().Value}";

            parser.ReceivedHeaderFrame += RecievedHeader;

            parser.GetHeaders();

            m_jsisCsvFileSource.Task.ContinueWith((t) => {
                parser.Stop();
            });
            return;
        }

        private void RecievedHeader(object sender, EventArgs<JsisCsvHeader> conf)
        {
            m_jsisCsvFileSource.SetResult(conf.Argument);

        }
        private void RecievedDataFrame(object sender, EventArgs<JsisCsvDataRow> conf)
        {
            bool a = m_dataQueue.Writer.TryWrite(conf.Argument);
        }

        private void ConnectionTerminated(object sender, EventArgs conf)
        {
            m_FileComplete.Set();
        }
        #endregion

        #region [ static ]
        private static Regex m_DateTimeParsing = new Regex(@"_([0-9]{8})_([0-9]{6})\.csv$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        #endregion
    }
}
