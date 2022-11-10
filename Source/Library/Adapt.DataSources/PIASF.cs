// ******************************************************************************************************
//  PiASF.tsx - Gbtc
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
//      Generated original version of source code.
//  09/08/2021 - J. Ritchie Carroll
//      Improved async read operations and host SDK process startup and shutdown
//
// ******************************************************************************************************

using Adapt.Models;
using AFSDKnetcore;
using AFSDKnetcore.AF;
using AFSDKnetcore.AF.Asset;
using AFSDKnetcore.AF.PI;
using AFSDKnetcore.AF.Search;
using Gemstone;
using GemstoneCommon;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading;
using static Common.Constants;

[assembly:InternalsVisibleTo("Adapt")]

namespace Adapt.DataSources
{
    /// <summary>
    /// Represents a data source adapter that imports data from a OSISoft PI Instance.
    /// </summary>
    [Description("OSISoft PI AF Historian: Imports Phasor data from a PI Instance with full AF metaData.")]
    public class PIASF : IDataSource
    {
        #region [ Members ]

        private const string AFSDKHost = "AFSDKhost";

        private PIServer m_server;
        private PIAFSDKSettings m_settings;
        private double m_progress;
        #endregion

        public Type SettingType => typeof(PIAFSDKSettings);

        public event EventHandler<MessageArgs> MessageRecieved;

        /// <summary>
        /// The PI DataSource does not support Progress Reports.
        /// </summary>
        /// <returns>Returns True.</returns>
        public bool SupportProgress => true;
        

        #region [ Methods ]

        public void Configure(IConfiguration config)
        {
            m_settings = new PIAFSDKSettings();
            config.Bind(m_settings);
        }

        public async IAsyncEnumerable<IFrame> GetData(List<AdaptSignal> signals, DateTime start, DateTime end)
        {
            if (m_server is null || !m_server.ConnectionInfo.IsConnected)
                ConnectPI();


            PIPointList pointList = new PIPointList(signals.Select(s => PIPoint.FindPIPoint(m_server, s.ID)));

          
            long startTicks = start.Ticks;
            double totalTicks = end.Ticks - startTicks;
            m_progress = 0.0D;


            TimeOrderedValueReader reader = new()
            {
                Points = pointList,
                StartTime = start,
                EndTime = end
            };

            long currentTime = 0;
            long nextReport = 0;
            Dictionary<string, ITimeSeriesValue> data = new Dictionary<string, ITimeSeriesValue>();

            await foreach (AFValue value in reader.ReadAsync())
            {
                if (value.Value is Exception ex)
                    throw ex;

                if (currentTime != value.Timestamp.UtcTime.Ticks)
                {
                    if (currentTime > nextReport)
                    {
                        LogInfo($"Completed reading data for {new DateTime(currentTime).ToLongTimeString()}");
                        nextReport = currentTime + Ticks.PerMinute * 30;
                    }

                    yield return new Frame()
                    {
                        Published = true,
                        Timestamp = value.Timestamp.UtcTime.Ticks,
                        Measurements = new ConcurrentDictionary<string, ITimeSeriesValue>(data)
                    };
                    m_progress = (currentTime - startTicks) / totalTicks;
                    data = new Dictionary<string, ITimeSeriesValue>();
                    currentTime = value.Timestamp.UtcTime.Ticks;
                }
                if (data.ContainsKey(value.PIPoint.Name))
                    continue;
                data.Add(value.PIPoint.Name, new AdaptValue(value.PIPoint.Name, Convert.ToDouble(value.Value), currentTime));
            }
                           
            m_progress = 1.0D;
        }

        public IEnumerable<AdaptDevice> GetDevices()
        {
            return LoadElements().Select(item => new AdaptDevice(item.UniqueID, item.Name));
        }

        private List<AFElement> LoadElements()
        {
            try
            {
                if (m_server is null || !m_server.ConnectionInfo.IsConnected)
                    ConnectPI();

                // Pull AF Data - This is New
                AFDatabase m_database = m_server.Databases[m_settings.InstanceName];

                List<AFElement> pmus = new List<AFElement>();

                using (AFElementSearch elementquery = new AFElementSearch(m_database, "ElementSearch", m_settings.Filter))
                {
                    elementquery.CacheInterval = TimeSpan.FromMinutes(10);
                    foreach (AFElement item in elementquery.FindObjects(fullLoad: true))
                    {
                        pmus.Add(item);
                    }
                }

                return pmus;
            }
            catch (Exception ex)
            {
                LogError("Unable to retrieve PMUs from PI Server", ex);
                return new List<AFElement>();
            }
        }

        /// <summary>
        /// Progress Reports are not supported from the OH Data Source
        /// </summary>
        /// <returns> This call Will Fail. </returns>
        public double GetProgress()
        {
            return m_progress * 100.0D;
        }


        /// <summary>
        /// The <see cref="IDataSource.GetSignals"/> for the PI data source
        /// </summary>
        /// <returns> A List of all available Signals in the Pi Instance.</returns>
        public IEnumerable<AdaptSignal> GetSignals()
        {
            List<AdaptSignal> signals = new List<AdaptSignal>();
           
            foreach (AFElement pmu in LoadElements())
            {
                int fps;
                if (!int.TryParse(pmu.Attributes["FrameRate"].Value.ToString(), out fps))
                    fps = 30;
                try
                {
                    signals.Add(new AdaptSignal(pmu.Attributes["Frequency"].PIPoint.Name, "Frequency", pmu.UniqueID, fps)
                    {
                        Phase = Phase.NONE,
                        Type = MeasurementType.Frequency
                    });
                }
                catch (Exception ex)
                {
                    LogInfo($"Unable to get Frequency for PMU {pmu.Name}", ex);
                }
                try
                {
                    signals.Add(new AdaptSignal(pmu.Attributes["DfDT"].PIPoint.Name, "ROCOF", pmu.UniqueID, fps)
                    {
                        Phase = Phase.NONE,
                        Type = MeasurementType.DeltaFrequency
                    });
                }
                catch (Exception ex)
                {
                    LogInfo($"Unable to get DfDT for PMU {pmu.Name}", ex);
                }

                foreach (AFElement phasor in pmu.Elements)
                {
                    try
                    {
                        signals.Add(new AdaptSignal(phasor.Attributes["Magnitude"].PIPoint.Name, phasor.Name + " Magnitude", pmu.UniqueID, fps )
                        {
                            Phase = ConvertPhase(phasor.Attributes["Phase"].Value.ToString()),
                            Type = ConvertType(phasor.Attributes["Type"].Value.ToString(), true)
                        });
                    }
                    catch (Exception ex)
                    {
                        LogInfo($"Unable to get Magnitude for PMU {pmu.Name} Phasor: ${phasor.Name}", ex);
                    }
                    try
                    {
                        signals.Add(new AdaptSignal(phasor.Attributes["Angle"].PIPoint.Name, phasor.Name + " Phase", pmu.UniqueID, fps)
                        {
                            Phase = ConvertPhase(phasor.Attributes["Phase"].Value.ToString()),
                            Type = ConvertType(phasor.Attributes["Type"].Value.ToString(), false)
                        });
                    }
                    catch (Exception ex)
                    {
                        LogInfo($"Unable to get Phase for PMU {pmu.Name} Phasor: ${phasor.Name}",ex);
                    }
                }
            }

            return signals;
        }

        private Phase ConvertPhase(string p)
        {

            p = p.Trim().ToUpper();
            if (p == "A" || p == "AN")
                return Phase.A;
            if (p == "B" || p == "BN")
                return Phase.B;
            if (p == "C" || p == "CN")
                return Phase.C;
            if (p == "AB")
                return Phase.AB;
            if (p == "BC")
                return Phase.BC;
            if (p == "CA")
                return Phase.CA;
            if (p == "N" || p == "NEUTRAL")
                return Phase.N;
            if (p == "NEG" || p == "-")
                return Phase.Neg;
            if (p == "POS" || p == "+")
                return Phase.Pos;
            if (p == "ZERO" || p == "0")
                return Phase.Zero;
            return Phase.NONE;
        }

        private MeasurementType ConvertType(string Type, bool isMag)
        {
            Type = Type.Trim().ToUpper();
            if (Type == "VOLTAGE" || Type == "V")
                return (isMag ? MeasurementType.VoltageMagnitude : MeasurementType.VoltagePhase);
            if (Type == "CURRENT" || Type == "I")
                return (isMag ? MeasurementType.CurrentMagnitude : MeasurementType.CurrentPhase);
            if (Type == "FREQUENCY" || Type == "F" || Type == "FREQ")
                return MeasurementType.Frequency;
            return MeasurementType.Other;

        }

       

        /// <summary>
        /// The Test Function required by <see cref="IDataSource.Test"/>
        /// </summary>
        /// <returns> A boolean indicating if it was able to connect to the PI and the requested Instance exists.</returns>
        public bool Test()
        {
            try
            {
                ConnectPI();
                return true;
            }
            catch (Exception ex)
            {
                LogError("Unable to Connect to PI", ex);
                return false;
            }
        }

        private void ConnectPI()
        {
            InitializeHost();

            // Locate configured PI server
            PIServers servers = new PIServers();
            m_server = servers[m_settings.ServerName];

            if (m_server is null)
                throw new InvalidOperationException("Server not found in the PI servers collection.");

            if (!m_server.ConnectionInfo.IsConnected)
            {
                // Attempt to open OSI-PI connection
                m_server.Connect(true);
            }
        }

        private void LogInfo(string message, Exception ex=null)
        {
            MessageRecieved?.Invoke(this, new MessageArgs(message, ex, MessageArgs.MessageLevel.Info));
        }

        private void LogError(string message, Exception ex)
        {
            MessageRecieved?.Invoke(this, new MessageArgs(message, ex, MessageArgs.MessageLevel.Error));
        }

        /// <summary>
        /// Function to start AFSDKHost if necessary
        /// </summary>

        #endregion

        #region [ static ]

        private static Process s_afSDKHost;

        internal static void InitializeHost()
        {
            // Make sure AFSDK host application is running
            bool hostIsRunning = false;

            foreach (TcpConnectionInformation info in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections())
            {
                if (info.LocalEndPoint.Port == DefaultPort)
                {
                    hostIsRunning = true;
                    break;
                }
            }

            if (!hostIsRunning)
            {
                lock (typeof(PIASF))
                {
                    if (s_afSDKHost is null)
                    {
                        string hostApp = Path.GetFullPath($@".\{AFSDKHost}\{AFSDKHost}.exe");

                        if (!File.Exists(hostApp))
                            throw new InvalidOperationException($"Failed to find \"{AFSDKHost}.exe\" build for testing, check path: {hostApp}");

                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true,
                            FileName = hostApp
                            //Arguments = SDKHostPort.ToString()
                        };

                        s_afSDKHost = Process.Start(startInfo);
                    }
                }
            }

            // Make sure .NET core AFSDK API is initialized
            if (!API.Initialized)
                API.Initialize("localhost");
        }

        internal static void ShutDownHost()
        {
            lock (typeof(PIASF))
            {
                try
                {
                    Debug.WriteLine($"Attempting to shutdown {AFSDKHost}.exe");

                    s_afSDKHost?.Close();
                    s_afSDKHost = null;

                    IEnumerable<Process> hostProcesses = Process.GetProcesses().Where(
                        process => process.ProcessName.Equals(AFSDKHost, StringComparison.OrdinalIgnoreCase));
    
                    foreach (Process process in hostProcesses)
                        process.Kill();
                }
                catch
                {
                    // Not failing because process cannot be stopped
                }
            }
        }

        #endregion
    }
}
