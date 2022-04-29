// ******************************************************************************************************
//  PdatImport.tsx - Gbtc
//
//  Copyright © 2021, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  03/29/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Adapt.Models;
using Gemstone;
using GemstoneCommon;
using GemstonePhasorProtocolls;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Adapt.DataSources
{
    /// <summary>
    /// Represents a data source adapter that imports data from pdat Files.
    /// </summary>
    [Description("PDAT Import: Imports data from a set of pdat files.")]
    public class PdatImporter : IDataSource
    {
        
        #region [ Members ]
        private PdatSettings m_settings;
        private Dictionary<DateTime, string> m_Files = new Dictionary<DateTime, string>();
        private Channel<IDataFrame> m_dataQueue;
        private ManualResetEvent m_FileComplete;
        public TaskCompletionSource<IConfigurationFrame> m_ConfigFrameSource;
        private int n_files;
        private int n_filesProcessed;

        #endregion

        public Type SettingType => typeof(PdatSettings);

        public event EventHandler<MessageArgs> MessageRecieved;

        public bool SupportProgress => true;



        #region [ Constructor ]

        public PdatImporter()
        {
            m_ConfigFrameSource = new TaskCompletionSource<IConfigurationFrame>();
        }
        #endregion

        #region [ Methods ]
        public void Configure(IConfiguration config)
        {
            m_ConfigFrameSource = new TaskCompletionSource<IConfigurationFrame>();
            m_settings = new PdatSettings();
            config.Bind(m_settings);

            m_Files = new Dictionary<DateTime, string>();

            // Find all Files and parse into dateTime
            if (!Directory.Exists(m_settings.RootFolder))
            {
                m_ConfigFrameSource.SetResult(null);
                return;
            }

                List <string> files = Directory.GetFiles(m_settings.RootFolder,"*.pdat",SearchOption.AllDirectories).ToList();

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


            GetConfigFrame();
        }

        public async IAsyncEnumerable<IFrame> GetData(List<AdaptSignal> signals, DateTime start, DateTime end)
        {
            n_filesProcessed = 0;
            n_files = 0;
            //Create List of Relevant Files
            List<DateTime> files = m_Files.Keys.Where(k => k < end && k > start).ToList();

            if (m_Files.Keys.Where(k => k <= start).Count() > 0)
            {
                DateTime initial = start - m_Files.Keys.Where(k => k <= start).Min(k => (start - k));

                if ((start - initial).TotalMinutes < m_settings.MaxFileLength)
                    files.Add(initial);
            }

            if (files.Count() == 0)
                yield break;

            //Start File Read Process
            m_dataQueue = Channel.CreateUnbounded<IDataFrame>();
            files.Sort();

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            ReadFile(files, tokenSource.Token).Start();

            await foreach(IDataFrame frame in m_dataQueue.Reader.ReadAllAsync())
            {
                if (frame.Timestamp < new Ticks(start))
                    continue;
                if (frame.Timestamp > new Ticks(end))
                {
                    tokenSource.Cancel();
                    continue;
                }

                IEnumerable<ITimeSeriesValue> magnitudes = frame.Cells.SelectMany(item => item.PhasorValues)
                    .Select(item => new Tuple<int, IPhasorValue>(signals.FindIndex(s => s.Device == item.Parent.IDCode.ToString() && s.ID == item.Parent.IDCode.ToString() + ":" +  item.Label + "-Mag"),item))
                    .Where(item => item.Item1 != -1)
                    .Select(item => new AdaptValue(signals[item.Item1].ID) { 
                        Value=item.Item2.Magnitude,
                        Timestamp=frame.Timestamp,
                    });

                IEnumerable<ITimeSeriesValue> phases = frame.Cells.SelectMany(item => item.PhasorValues)
                    .Select(item => new Tuple<int, IPhasorValue>(signals.FindIndex(s => s.Device == item.Parent.IDCode.ToString() && s.ID == item.Parent.IDCode.ToString() + ":" + item.Label + "-Ph"), item))
                    .Where(item => item.Item1 != -1)
                    .Select(item => new AdaptValue(signals[item.Item1].ID)
                    {
                        Value = item.Item2.Angle.ToDegrees(),
                        Timestamp = frame.Timestamp,
                    });

                IEnumerable<ITimeSeriesValue> analogs = frame.Cells.SelectMany(item => item.AnalogValues)
                    .Select(item => new Tuple<int, IAnalogValue>(signals.FindIndex(s => s.Device == item.Parent.IDCode.ToString() && s.ID == item.Parent.IDCode.ToString() + ":" + item.Label), item))
                    .Where(item => item.Item1 != -1)
                    .Select(item => new AdaptValue(signals[item.Item1].ID)
                    {
                        Value = item.Item2.Value,
                        Timestamp = frame.Timestamp,
                    });

                IEnumerable<ITimeSeriesValue> frequencies = frame.Cells.Select(item => item.FrequencyValue)
                    .Select(item => new Tuple<int, IFrequencyValue>(signals.FindIndex(s => s.Device == item.Parent.IDCode.ToString() && s.ID == item.Parent.IDCode.ToString() + item.Label ), item))
                    .Where(item => item.Item1 != -1)
                    .Select(item => new AdaptValue(signals[item.Item1].ID)
                    {
                        Value = item.Item2.Frequency,
                        Timestamp = frame.Timestamp,
                    }); ;

                IEnumerable<ITimeSeriesValue> digitals = frame.Cells.SelectMany(item => item.DigitalValues)
                    .Select(item => new Tuple<int, IDigitalValue>(signals.FindIndex(s => s.Device == item.Parent.IDCode.ToString() && s.ID == item.Parent.IDCode.ToString() + ":" +  item.Label), item))
                    .Where(item => item.Item1 != -1)
                    .Select(item => new AdaptValue(signals[item.Item1].ID)
                    {
                        Value = item.Item2.Value,
                        Timestamp = frame.Timestamp,
                    });

                IFrame outFrame = new Frame()
                {
                    Published = true,
                    Timestamp = frame.Timestamp,
                    Measurements = new ConcurrentDictionary<string, ITimeSeriesValue>(phases.Concat(magnitudes).Concat(analogs).Concat(digitals).Concat(frequencies).Select(item => new KeyValuePair<string, ITimeSeriesValue>(item.ID, item)))
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

                // 10 Threads for now. We can increase this in the future if necessary
                List<Tuple<Channel<IDataFrame>,int,string>> readers = new List<Tuple<Channel<IDataFrame>,int,string>>();
                List<string> fileNames = files.Select((d) => m_Files[d]).ToList();

                int n_readers = Math.Min(files.Count(), 10);
                int n_files = (int)Math.Ceiling((double)files.Count() / (double)n_readers);

                for (int i = 0; i < n_readers; i++)
                {
                    List<string> activeFileNames = fileNames.Skip(i * n_files).Take(n_files).ToList();
                    readers.Add(new Tuple<Channel<IDataFrame>,int,string>(Channel.CreateUnbounded<IDataFrame>(), activeFileNames.Count(), activeFileNames.Last() ));
                    
                    GenerateReader(activeFileNames, readers[i],cancellationToken);
                }
                CombineQueues(readers).Wait();
                m_dataQueue.Writer.Complete();
            }, cancellationToken);

        }

        public IEnumerable<AdaptDevice> GetDevices()
        {
            if (!m_ConfigFrameSource.Task.IsCompleted && m_ConfigFrameSource.Task.Status != TaskStatus.WaitingForActivation)
                return new List<AdaptDevice>();

            if (m_ConfigFrameSource.Task.Result == null)
                return new List<AdaptDevice>();

            IConfigurationFrame configuration = m_ConfigFrameSource.Task.Result;

            return configuration.Cells.Select(cell => new AdaptDevice(cell.IDCode.ToString(),cell.StationName)).ToList();
        }

        public double GetProgress()
        {
            return ((double)n_filesProcessed / (double)(n_files)*100.0D);
        }

        private async Task CombineQueues(List<Tuple<Channel<IDataFrame>, int, string>> queues)
        {
            foreach (Tuple<Channel<IDataFrame>, int, string> channel in queues)
            {
                IDataFrame point;
                while (await channel.Item1.Reader.WaitToReadAsync())
                {
                    if (!channel.Item1.Reader.TryRead(out point))
                        continue;
                    m_dataQueue.Writer.TryWrite(point);
                   
                }

                LogInfo($"aligning File {channel.Item3} complete");
                n_filesProcessed += channel.Item2;
            }
        }

        private void GenerateReader(List<string> files, Tuple<Channel<IDataFrame>, int, string> reader, CancellationToken cancellationToken)
        {
            Task.Run(() => {

                n_files = files.Count();

                for (int i = 0; i < files.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    MultiProtocolFrameParser parser = new MultiProtocolFrameParser();
                    ManualResetEvent fileComplete = new ManualResetEvent(false);
                    parser.PhasorProtocol = PhasorProtocol.IEEEC37_Pdat;
                    parser.TransportProtocol = Gemstone.Communication.TransportProtocol.File;
                    parser.ConnectionString = $"file={files[i]}";
                    LogInfo($"reading File: {files[i]}");
                    parser.CompletedFileParsing += (object sender, EventArgs<int> conf) => { fileComplete.Set(); };
                    parser.ReceivedDataFrame += (object sender, EventArgs<IDataFrame> conf) => {
                        reader.Item1.Writer.TryWrite(conf.Argument);
                    };
                    parser.DefinedFrameRate = 10000000;
                    parser.DisconnectAtEOF = true;
                    parser.ParsingException += ParsingException;
                    parser.Start();
                    WaitHandle.WaitAny(new WaitHandle[] { fileComplete, cancellationToken.WaitHandle }, 5000);
                    parser.Stop();
                    LogInfo($"Reading File {files[i]} complete");
                }
                reader.Item1.Writer.Complete();

            },cancellationToken);

        }
        public IEnumerable<AdaptSignal> GetSignals()
        {
            if (!m_ConfigFrameSource.Task.IsCompleted && m_ConfigFrameSource.Task.Status != TaskStatus.WaitingForActivation)
                return new List<AdaptSignal>();
            
            IConfigurationFrame configuration = m_ConfigFrameSource.Task.Result;

            if (configuration == null)
                return new List<AdaptSignal>();

            IEnumerable<AdaptSignal> analogs = configuration.Cells.SelectMany(cell => cell.AnalogDefinitions
                .Select(aD => new AdaptSignal(cell.IDCode.ToString() + ":" + aD.Label, aD.Label, cell.IDCode.ToString(), cell.FrameRate)
                    {
                        FramesPerSecond = cell.FrameRate,
                        Phase = Phase.NONE,
                        Type = MeasurementType.Analog
                    }));

            IEnumerable<AdaptSignal> digitals = configuration.Cells.SelectMany(cell => cell.DigitalDefinitions
                .Select(aD => new AdaptSignal(cell.IDCode.ToString() + ":" + aD.Label, aD.Label, cell.IDCode.ToString(), cell.FrameRate)
                {
                    FramesPerSecond = cell.FrameRate,
                    Phase = Phase.NONE,
                    Type = MeasurementType.Digital
                }));

            IEnumerable<AdaptSignal> phases = configuration.Cells.SelectMany(cell => cell.PhasorDefinitions
                .Select(aD => new AdaptSignal(cell.IDCode.ToString() + ":" + aD.Label + "-Ph", aD.Label + " Phase", cell.IDCode.ToString(), cell.FrameRate)
                {
                    FramesPerSecond = cell.FrameRate,
                    Phase = Phase.NONE,
                    Type = aD.PhasorType == Gemstone.Numeric.EE.PhasorType.Current? MeasurementType.CurrentPhase : MeasurementType.VoltagePhase
                }));
            IEnumerable<AdaptSignal> magnitudes = configuration.Cells.SelectMany(cell => cell.PhasorDefinitions
            .Select(aD => new AdaptSignal(cell.IDCode.ToString() + ":" + aD.Label + "-Mag", aD.Label + " Magnitude", cell.IDCode.ToString(), cell.FrameRate) 
            {
                FramesPerSecond = cell.FrameRate,
                Phase = Phase.NONE,
                Type = aD.PhasorType == Gemstone.Numeric.EE.PhasorType.Current ? MeasurementType.CurrentMagnitude : MeasurementType.VoltageMagnitude
            }));

            IEnumerable<AdaptSignal> frequency = configuration.Cells.Select( cell => new AdaptSignal(cell.IDCode.ToString() + cell.FrequencyDefinition.Label, cell.FrequencyDefinition.Label ?? "Frequency", cell.IDCode.ToString(), cell.FrameRate)
               {
                   FramesPerSecond = cell.FrameRate,
                   Phase = Phase.NONE,
                   Type =MeasurementType.Frequency,
               });

            return magnitudes.Concat(phases).Concat(digitals).Concat(analogs).Concat(frequency);
        }

   
        public bool Test()
        {
            if (!Directory.Exists(m_settings.RootFolder))
                return false;

            return m_Files.Count > 0;
        }

        private void GetConfigFrame()
        {
            if (m_Files.Count == 0)
                m_ConfigFrameSource.SetResult(null);

            if (m_ConfigFrameSource.Task.IsCompleted)
                return;
            
            MultiProtocolFrameParser parser = new MultiProtocolFrameParser();
            parser.PhasorProtocol = PhasorProtocol.IEEEC37_Pdat;
            parser.TransportProtocol = Gemstone.Communication.TransportProtocol.File;
            parser.ConnectionString = $"file={m_Files.First().Value}";

            parser.ReceivedConfigurationFrame += RecievedConfigFrame;
            parser.Start();

            m_ConfigFrameSource.Task.ContinueWith((t) => {
                parser.Stop();
            });
            return;
        }

        private void RecievedConfigFrame(object sender, EventArgs<IConfigurationFrame> conf)
        {
            m_ConfigFrameSource.SetResult(conf.Argument);

        }

        private void RecievedDataFrame(object sender, EventArgs<IDataFrame> conf)
        {
            bool a = m_dataQueue.Writer.TryWrite(conf.Argument);
        }

        private void CompletedFileParsing(object sender, EventArgs conf)
        {
            m_FileComplete.Set();
        }

        private void ParsingException(object sender, EventArgs<Exception> conf)
        {
            LogError($"Unable to parse pdat File: {conf.Argument.Message}");
        }

        private void LogInfo(string message)
        {
            MessageRecieved?.Invoke(this, new MessageArgs(message, MessageArgs.MessageLevel.Info));
        }

        private void LogError(string message)
        {
            MessageRecieved?.Invoke(this, new MessageArgs(message, MessageArgs.MessageLevel.Error));
        }
        #endregion

        #region [ static ]
        private static Regex m_DateTimeParsing = new Regex(@"_([0-9]{8})_([0-9]{6})\.pdat$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #endregion
    }
}
