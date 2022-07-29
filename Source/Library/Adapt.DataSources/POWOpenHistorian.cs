// ******************************************************************************************************
//  OpenHistorian.tsx - Gbtc
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
//  06/02/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Adapt.Models;
using Gemstone;
using Gemstone.Collections.CollectionExtensions;
using Gemstone.Numeric.Analysis;
using GemstoneCommon;
using GemstonePhasorProtocolls;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Adapt.DataSources
{
    /// <summary>
    /// Represents a data source adapter that imports data from an openHistorian Instance.
    /// Note that this requires OH 2.8.5+
    /// </summary>
    [Description("POW OpenHistorian: Imports Point on Wave data from an OpenHistorian.")]
    public class POWOpenHistorian : IDataSource
    {
        #region [ Classes ]
        [Serializable]

        
        public class HistorianAggregatePoint
        {
            public string Tag { get; set; }
            public string Timestamp { get; set; }
            public double Minimum { get; set; }
            public double Average { get; set; }
            public double Maximum { get; set; }
            public ulong QualityFlags { get; set; }

        }

        public struct PowSignal
        {
            public string ID;
            public bool IncludePhase;
            public bool IncludeMagnitude;
        }

        /// <summary>
        /// Body of post queries to Historian
        /// </summary>
        public class Post
        {
            /// <summary>
            /// The acronym of the historian instance to query from
            /// </summary>
            public string Instance { get; set; }
            /// <summary>
            /// How the result is aggregated 1s,1m,1d,1w
            /// </summary>
            public string Aggregate { get; set; }
            /// <summary>
            /// List of device names to include in query results
            /// </summary>
            public string[] Devices { get; set; }
            /// <summary>
            /// List of Phases to include in query results
            /// </summary>
            public string[] Phases { get; set; }
            /// <summary>
            /// List of SignalTypes to include in query results
            /// </summary>
            public string[] Types { get; set; }
            /// <summary>
            /// Bit flag of valid Hours of the day to include in query results
            /// </summary>
            public ulong Hours { get; set; }
            /// <summary>
            /// Bit flag of valid Days of the week to include in query results
            /// </summary>
            public ulong Days { get; set; }
            /// <summary>
            /// Bit flag of valid Weeks of the year to include in query results
            /// </summary>
            public ulong Weeks { get; set; }
            /// <summary>
            /// Bit flag of valid Months of the year to include in query results
            /// </summary>
            public ulong Months { get; set; }
            /// <summary>
            /// Start time of query results
            /// </summary>
            public DateTime StartTime { get; set; }
            /// <summary>
            /// End time of query results
            /// </summary>
            public DateTime EndTime { get; set; }

            ///<summary>
            ///  List of Point IDs to include in query results
            ///  Note that this will override <see cref="Types"/>, <see cref="Phases"/> and <see cref="Devices"/>
            ///</summary>
            public string[]? Tags { get; set; }

        }

        #endregion

        #region [ Members ]

        private POWOpenHistorianSettings m_settings;
        private List<AdaptDevice> m_Devices = null;
        private List<AdaptSignal> m_Signals = null;

        private Channel<Tuple<DateTime, Dictionary<string,double>>> m_DataQueue;
        private Channel<IFrame> m_OutputQueue;

        private DateTime m_epoch = new DateTime(1970, 1, 1);
        private static readonly HttpClient client = new HttpClient();

        const ulong ValidHours = 16777215; // Math.Pow(2, 24) - 1
        const ulong ValidDays = 127; //  (int)(Math.Pow(2, 7) - 1);
        const ulong ValidWeeks = 9007199254740991; // (int)(Math.Pow(2, 53) - 1);
        const ulong ValidMonths = 4095;//  (int)(Math.Pow(2, 12) - 1);

        #endregion

        public event EventHandler<MessageArgs> MessageRecieved;
        public Type SettingType => typeof(POWOpenHistorianSettings);

        /// <summary>
        /// The OH DataSource does not support Progress Reports.
        /// </summary>
        /// <returns>Returns False.</returns>
        public bool SupportProgress => false;

        #region [ Constructor ]

        public POWOpenHistorian()
        {}
        #endregion

        #region [ Methods ]
        public void Configure(IConfiguration config)
        {
            m_settings = new POWOpenHistorianSettings();
            config.Bind(m_settings);

            m_settings.Server = m_settings.Server.Trim();
            m_settings.Server = m_settings.Server.TrimEnd('/');
            
        }

        public async IAsyncEnumerable<IFrame> GetData(List<AdaptSignal> signals, DateTime start, DateTime end)
        {
            m_OutputQueue = Channel.CreateUnbounded<IFrame>();

            // Generate POW Signals
            List<PowSignal> powSignals = new List<PowSignal>();

            foreach (AdaptSignal sig in signals)
            {
                bool isPhase = false;
                string powSignal = sig.ID;
                if (powSignal.EndsWith("Phase"))
                {
                    powSignal = powSignal.Substring(0, powSignal.Length - "Phase".Length);
                    isPhase = true;
                }
                else
                    powSignal = powSignal.Substring(0, powSignal.Length - "Magnitude".Length);
                
                int i = powSignals.FindIndex((s) => s.ID == powSignal);
                if (i == -1)
                    powSignals.Add(new PowSignal() { ID = powSignal, IncludeMagnitude = !isPhase, IncludePhase = isPhase });
                else if (isPhase)
                    powSignals[i] = new PowSignal() { IncludePhase = true, ID = powSignals[i].ID, IncludeMagnitude = powSignals[i].IncludeMagnitude };
                else
                    powSignals[i] = new PowSignal() { IncludePhase = powSignals[i].IncludePhase, ID = powSignals[i].ID, IncludeMagnitude = true };

            }
                

            // Start Polling
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            ReadPOW(start, end, powSignals, tokenSource.Token).Start();

            // Start Computation of Phasor
            ComputePhasor(start, powSignals).Start();

            // Send results
            await foreach (IFrame frame in m_OutputQueue.Reader.ReadAllAsync())
            {
                yield return frame;
            }

        }

        public Task ReadPOW(DateTime start, DateTime end, List<PowSignal> signals, CancellationToken cancellationToken)
        {
            m_DataQueue = Channel.CreateUnbounded<Tuple<DateTime, Dictionary<string, double>>>();

            return new Task(() => {

            string token = GenerateAntiForgeryToken();
            DateTime section = start;
            DateTime sectionEnd = section.AddMinutes(5);

            sectionEnd = (sectionEnd > end ? end : sectionEnd);
                while (section < end)
                {
                    try
                    {
                        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, m_settings.Server + "/api/trendap/Query"))
                        {
                            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{m_settings.User}:{m_settings.Password}")));

                            request.Headers.Add("X-GSF-Verify", token);


                            Post post = new Post()
                            {
                                Instance = m_settings.Instance,
                                Aggregate = "*",
                                Devices = new string[] { },
                                Phases = new string[] { },
                                Types = new string[] { },
                                Hours = ValidHours,
                                Days = ValidDays,
                                Weeks = ValidWeeks,
                                Months = ValidMonths,
                                StartTime = section,
                                EndTime = sectionEnd,
                                Tags = signals.Select(s => s.ID).ToArray()
                            };

                            HttpContent content = JsonContent.Create(post);

                            request.Content = content;

                            using (HttpResponseMessage response = client.SendAsync(request).Result)
                            {

                                Task<string> rsp = response.Content.ReadAsStringAsync();

                                IEnumerable<HistorianAggregatePoint> points = JsonConvert.DeserializeObject<IEnumerable<HistorianAggregatePoint>>(rsp.Result);
                                foreach (IGrouping<string, HistorianAggregatePoint> grp in points.GroupBy(item => item.Timestamp))
                                {

                                    DateTime TS = DateTime.Parse(grp.Key).ToUniversalTime();

                                    Tuple<DateTime, Dictionary<string, double>> data = new Tuple<DateTime, Dictionary<string, double>>(TS, new Dictionary<string, double>());

                                    foreach (HistorianAggregatePoint pt in grp)
                                    {
                                        data.Item2.Add(pt.Tag, pt.Average);

                                    }
                                    m_DataQueue.Writer.TryWrite(data);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Unable to retrieve Timeslice {section.ToShortDateString()} {section.ToShortTimeString()}to {sectionEnd.ToShortDateString()} {sectionEnd.ToShortTimeString()}");
                    }

                    section = sectionEnd.AddTicks(1);
                    sectionEnd = section.AddMinutes(5);
                    sectionEnd = (sectionEnd > end ? end : sectionEnd);
                }

                m_DataQueue.Writer.Complete();
                
            }, cancellationToken);

        }

        private Task ComputePhasor(DateTime start, List<PowSignal> signals)
        {
            return new Task(async () =>
            {
                DateTime ts = Ticks.AlignToSubsecondDistribution(start, (int)m_settings.SamplingFrequency, 0);

                // need to grab the pow signals

                List<Tuple<DateTime, Dictionary<string, double>>> data = new List<Tuple<DateTime, Dictionary<string, double>>>();

                while (await m_DataQueue.Reader.WaitToReadAsync())
                {
                    Tuple<DateTime, Dictionary<string, double>> point;
                    if (!m_DataQueue.Reader.TryRead(out point))
                        continue;

                    Tuple<DateTime, List<Tuple<DateTime, Dictionary<string, double>>>> r = ProcessPoint(point, data, ts, signals);
                    ts = r.Item1;
                    data = r.Item2;
                }

                m_OutputQueue.Writer.Complete();
            });
        }

        private Tuple<DateTime, List<Tuple<DateTime, Dictionary<string, double>>>>  ProcessPoint(Tuple<DateTime, Dictionary<string, double>> point, List<Tuple<DateTime, Dictionary<string, double>>> data, DateTime currentTS, List<PowSignal> powSignals)
        {

            data.Add(point);
         
            if (point.Item1 < currentTS.AddSeconds(0.5D*m_settings.WindowSize))
                return new Tuple<DateTime, List<Tuple<DateTime, Dictionary<string, double>>>> (currentTS, data);

            data = data.Where(item => item.Item1 > currentTS.AddSeconds(-0.5D * m_settings.WindowSize)).ToList();

            if (data.Count() > 0)
            {
                Dictionary<string, ITimeSeriesValue> result = new Dictionary<string, ITimeSeriesValue>();

                foreach (PowSignal signal in powSignals)
                {
                    SineWave sineFit = WaveFit.SineFit(data.Select(d => d.Item2[signal.ID]).ToArray(), data.Select(d => (d.Item1 - m_epoch).TotalSeconds).ToArray(), 60.0D);
                    if (signal.IncludeMagnitude)
                        result.Add(signal.ID + "Magnitude", new AdaptValue(signal.ID + "Magnitude", sineFit.Amplitude, currentTS));
                    if (signal.IncludePhase)
                        result.Add(signal.ID + "Phase", new AdaptValue(signal.ID + "Phase", sineFit.Phase, currentTS));

                }

                IFrame frame = new Frame()
                {
                    Published = true,
                    Timestamp = currentTS,
                    Measurements = new ConcurrentDictionary<string, ITimeSeriesValue>(result)
                };

                m_OutputQueue.Writer.TryWrite(frame);
            }
            else
            {
                LogError($"no point on wave data available for {currentTS.ToShortDateString()} {currentTS.ToShortTimeString()}");

            }
            return new Tuple<DateTime, List<Tuple<DateTime, Dictionary<string, double>>>>(currentTS.AddTicks((long)((1.0D / m_settings.SamplingFrequency) * (double)Ticks.PerSecond)), data);
        }

        public IEnumerable<AdaptDevice> GetDevices()
        {
            if ((object)m_Devices == null)
                GetMetaData();
            return m_Devices;
        }

        /// <summary>
        /// Progress Reports are not supported from the OH Data Source
        /// </summary>
        /// <returns> This call Will Fail. </returns>
        public double GetProgress()
        {
            throw new NotImplementedException();
        }

      
        /// <summary>
        /// The <see cref="IDataSource.GetSignals"/> for the OH data source
        /// </summary>
        /// <returns> A List of all available Signals in the OpenHistorian Instance.</returns>
        public IEnumerable<AdaptSignal> GetSignals()
        {
            if ((object)m_Signals == null)
                GetMetaData();
            return m_Signals;

        }

        /// <summary>
        /// The Test Function required by <see cref="IDataSource.Test"/>
        /// </summary>
        /// <returns> A boolean indicating if it was able to connect to the OGH and the requested Instance exists.</returns>
        public bool Test()
        {
            return GetInstances();
        }

        /// <summary>
        /// Gets the Instances from the openHistorian Server and checks if the current <see cref="OpenHistorianSettings.Instance"/> is Valid
        /// </summary>
        /// <returns></returns>
        private bool GetInstances()
        {
            m_Devices = null;
            m_Signals = null;
            try
            {
                string rsp = GetTable("GetInstances");
                if (rsp == "")
                    return false;

                List<JObject> instances = JsonConvert.DeserializeObject<List<JObject>>(rsp);
                List<string> acronyms = instances.Select(item => item["Acronym"].ToString()).ToList();
                return acronyms.FindIndex(item => item == m_settings.Instance) > -1;
            }
            catch (InvalidOperationException ex)
            {
                return false;

            }
            catch (Exception ex)
            {
                return false;
            }
            
        }        
        
        /// <summary>
        /// Connect to the openHistorian Instance to get Devices and Signal Meta Data.
        /// </summary>
        private void GetMetaData()
        {
            m_Devices = new List<AdaptDevice>();
            m_Signals = new List<AdaptSignal>();

            string data = GetTable("GetMetaData");
            List<JObject> activeMeasurments = JsonConvert.DeserializeObject<List<JObject>>(data);
            activeMeasurments = activeMeasurments.Where(item => item["ID"].ToString().StartsWith(m_settings.Instance)).ToList();

            foreach (IGrouping<string, JObject> device in activeMeasurments.GroupBy(item => item["Device"].ToString()))
            {
                m_Devices.Add(new AdaptDevice(device.First()["Device"].ToString(), device.Key));

                foreach (JObject measurement in device)
                {
                    string name = measurement["PointTag"].ToString();
                    if (m_settings.NameField == NamingConvention.SignalReference)
                        name = measurement["SignalReference"].ToString();

                    int fps = int.Parse(measurement["FramesPerSecond"].ToString());
                    m_Signals.Add(
                        new AdaptSignal(measurement["ID"].ToString() + "Phase", name + " Ph", m_Devices.Last(), fps )
                        { 
                            Description = measurement["Description"].ToString(),
                            Phase = GetPhase(measurement["Phase"].ToString()),
                            Type = GetType(measurement["SignalType"].ToString(), true)
                        });
                    m_Signals.Add(
                        new AdaptSignal(measurement["ID"].ToString() + "Magnitude", name + " Mag", m_Devices.Last(), fps)
                        {
                            Description = measurement["Description"].ToString(),
                            Phase = GetPhase(measurement["Phase"].ToString()),
                            Type = GetType(measurement["SignalType"].ToString(), false)
                        });
                }
            }


        }

        /// <summary>
        /// Translates a OpenHistrorian Phase into a <see cref="Phase"/>
        /// </summary>
        /// <param name="phase">The OH Phase from the OH MetaData DB</param>
        /// <returns>The Corresponding <see cref="Phase"/>.</returns>
        private Phase GetPhase(string phase)
        {
            switch (phase)
            {
                case "A":
                    return Phase.A;
                case "B":
                    return Phase.B;
                case "C":
                    return Phase.C;
                case "+":
                    return Phase.Pos;
                case "-":
                    return Phase.Neg;
                case "0":
                    return Phase.Zero;
               
            }
            return Phase.NONE;
        }

        /// <summary>
        /// Translates a OpenHistorian Measurement into a <see cref="MeasurementType"/>
        /// </summary>
        /// <param name="phase">The OH Measurement Type from the OH MetaData DB</param>
        /// <returns>The Corresponding <see cref="MeasurementType"/>.</returns>
        private MeasurementType GetType(string type,bool phase)
        {
            switch (type)
            {
                case "IPHM":
                    return (phase? MeasurementType.CurrentPhase : MeasurementType.CurrentMagnitude);
                case "IPHA":
                    return (phase ? MeasurementType.CurrentPhase : MeasurementType.CurrentMagnitude);
                case "VPHM":
                    return (phase ? MeasurementType.VoltagePhase : MeasurementType.VoltageMagnitude);
                case "VPHA":
                    return (phase ? MeasurementType.VoltagePhase : MeasurementType.VoltageMagnitude);
                case "ALOG":
                    return MeasurementType.Analog;
            }
            return MeasurementType.Other;
        }

        /// <summary>
        /// Calls TrendAP API in openHistorian to grab table.
        /// </summary>
        /// <param name="Table">The name of the API endpoint.</param>
        /// <returns> Returns the response as a string.</returns>
        private string GetTable(string Table)
        {
            using (HttpClient client = new HttpClient())
            {
                    client.BaseAddress = new Uri(m_settings.Server);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{m_settings.User}:{m_settings.Password}")));
                    HttpResponseMessage response = client.GetAsync($"api/trendap/{Table}").Result;

                    if (!response.IsSuccessStatusCode)
                        return "";

                    return response.Content.ReadAsStringAsync().Result;              
            }
        }

        /// <summary>
        /// Generates an AntiForgeryToken when connection to he OH
        /// </summary>
        /// <returns></returns>
        private string GenerateAntiForgeryToken()
        {

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, m_settings.Server + "/api/rvht"))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(Encoding.ASCII.GetBytes($"{m_settings.User}:{m_settings.Password}")));

                using (HttpResponseMessage response = client.SendAsync(request).Result)
                {
                    if (!response.IsSuccessStatusCode)
                        return "";

                    Task<string> rsp = response.Content.ReadAsStringAsync();
                    return response.Content.ReadAsStringAsync().Result;
                }
            }
                
        }

        private void LogError(string message)
        {
            MessageRecieved?.Invoke(this, new MessageArgs(message, MessageArgs.MessageLevel.Error));
        }

        private void LogInfo(string message)
        {
            MessageRecieved?.Invoke(this, new MessageArgs(message, MessageArgs.MessageLevel.Info));
        }
        #endregion

        #region [ static ]

        #endregion
    }
}
