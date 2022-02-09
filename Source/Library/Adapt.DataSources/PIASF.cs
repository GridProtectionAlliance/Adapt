﻿// ******************************************************************************************************
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

            DateTime currentStart = start;
            DateTime currentEnd = start.AddMinutes(10);

            long startTicks = start.Ticks;
            double totalTicks = end.Ticks - startTicks;
            m_progress = 0.0D;

            while (currentStart < end)
            {
                PagedValueReader reader = new()
                {
                    Points = pointList,
                    StartTime = start,
                    EndTime = (currentEnd < end? currentEnd : end)
                };

                Dictionary<long, Dictionary<string, double>> data = new Dictionary<long, Dictionary<string, double>>();

                await foreach (AFValues values in reader.ReadAsync()) // <- Group of read values
                {
                    foreach (AFValue currentPoint in values)
                    {
                        long timestamp = currentPoint.Timestamp.UtcTime.Ticks;
                        string tag = currentPoint.PIPoint.Name;

                        if (!data.ContainsKey(timestamp))
                            data.Add(timestamp, new Dictionary<string, double>());

                        if (!data[timestamp].ContainsKey(tag))
                            data[timestamp].Add(tag, Convert.ToDouble(currentPoint.Value));
                        
                    }
                }
                foreach (long ts in data.Keys)
                {
                    Dictionary<string, ITimeSeriesValue> frameData = new Dictionary<string, ITimeSeriesValue>();
                    foreach (string tag in data[ts].Keys)
                    {
                        frameData.Add(tag, new AdaptValue(tag, data[ts][tag], ts));
                    }
                    yield return new Frame()
                    {
                        Published = true,
                        Timestamp = ts,
                        Measurements = new ConcurrentDictionary<string, ITimeSeriesValue>(frameData)
                    };
                }
                m_progress = (currentEnd.Ticks - startTicks) / totalTicks;
                currentStart = new DateTime(currentEnd.Ticks + 1);
                currentEnd = currentStart.AddMinutes(10); 
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

                signals.Add(new AdaptSignal(pmu.Attributes["Frequency"].PIPoint.Name, "Frequency", pmu.UniqueID, 30) 
                { 
                    Phase = Phase.NONE, 
                    Type=MeasurementType.Frequency
                });
                signals.Add(new AdaptSignal(pmu.Attributes["DfDT"].PIPoint.Name, "ROCOF", pmu.UniqueID, 30)
                {
                    Phase = Phase.NONE,
                    Type = MeasurementType.DeltaFrequency
                });

                foreach (AFElement phasor in pmu.Elements)
                {
                    signals.Add(new AdaptSignal(phasor.Attributes["Magnitude"].PIPoint.Name, phasor.Name + " Magnitude", pmu.UniqueID, 30)
                    {
                        Phase = ConvertPhase(phasor.Attributes["Phase"].Value.ToString()),
                        Type = ConvertType(phasor.Attributes["Type"].Value.ToString(), true)
                    });
                    signals.Add(new AdaptSignal(phasor.Attributes["Angle"].PIPoint.Name, phasor.Name + " Phase", pmu.UniqueID, 30)
                    {
                        Phase = ConvertPhase(phasor.Attributes["Phase"].Value.ToString()),
                        Type = ConvertType(phasor.Attributes["Type"].Value.ToString(), false)
                    });
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
        /// The PI DataSource does not support Progress Reports.
        /// </summary>
        /// <returns>Returns True.</returns>
        public bool SupportProgress()
        {
            return true;
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
            catch
            {
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