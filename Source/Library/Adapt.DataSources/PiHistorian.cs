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
using AFSDKnetcore;
using AFSDKnetcore.AF.Asset;
using AFSDKnetcore.AF.Data;
using AFSDKnetcore.AF.PI;
using AFSDKnetcore.AF.Time;
using Gemstone;
using Gemstone.Collections.CollectionExtensions;
using GemstoneCommon;
using GemstonePhasorProtocolls;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
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
    /// Represents a data source adapter that imports data from a OSISoft PI Instance.
    /// </summary>
    [Description("OSISoft PI Historian: Imports Phasor data from a PI Instance.")]
    public class PIHistorian : IDataSource
    {


        #region [ Members ]

        private const int DefaultPort = 20401;
        private PIServer m_server;
        private PIHistorianSettings m_settings;
        #endregion

        #region [ Constructor ]

        public PIHistorian()
        {}
        #endregion

        #region [ Methods ]
        public void Configure(IConfiguration config)
        {
            m_settings = new PIHistorianSettings();
            config.Bind(m_settings);
        }

        public async IAsyncEnumerable<IFrame> GetData(List<AdaptSignal> signals, DateTime start, DateTime end)
        {
            if (m_server is null || !m_server.ConnectionInfo.IsConnected)
                ConnectPI();

            PIPointList pointList = new()
            {
                PIPoint.FindPIPoint(m_server, m_settings.PITag)
            };

            PIPagingConfiguration pagingConfig = new(PIPageType.EventCount, 100);
            AFTimeRange timeRange = new AFTimeRange(start, end);

            IEnumerable<AFValues> results = pointList.RecordedValues(timeRange, AFBoundaryType.Inside, null, false, pagingConfig);

            // Logic only supports single PiTag at the moment.
            // We will need to use AF to get multiple based on PMU Name
            foreach (AFValues values in results) // <- Group of read values
            {

                foreach (AFValue currentPoint in values)
                {
                    long timestamp = currentPoint.Timestamp.UtcTime.Ticks;

                    Dictionary<string, ITimeSeriesValue> data = new Dictionary<string, ITimeSeriesValue>();
                    data.Add(m_settings.PITag, new AdaptValue(m_settings.PITag, Convert.ToDouble(currentPoint.Value), timestamp));

                    IFrame frame = new Frame()
                    {
                        Published = true,
                        Timestamp = timestamp,
                        Measurements = new ConcurrentDictionary<string, ITimeSeriesValue>(data)
                    };

                    yield return frame;
                }
                
            }



            /*
             using (PIConnection connection = new PIConnection() {
                 ServerName = m_settings.ServerName,
                 UserName = m_settings.UserName,
                 Password = m_settings.Password,
                 ConnectTimeout = m_settings.ConnectTimeout
             })
             {
                 if (!PIPoint.TryFindPIPoint(connection.Server, m_settings.PITag, out PIPoint point))
                     yield break;

                 AFTime startTime = start;
                 AFTime stopTime = end;

                 IEnumerator<AFValue> PIenumerator = new PIScanner
                 {
                     Points = new PIPointList() { point },
                     StartTime = startTime,
                     EndTime = stopTime,
                     DataReadExceptionHandler = ex => throw ex
                 }
                 .Read(m_settings.PageFactor).GetEnumerator(); ;


                 while (PIenumerator.MoveNext())
                 {
                     AFValue currentPoint = PIenumerator.Current;

                     if ((object)currentPoint == null)
                         throw new NullReferenceException("PI data read returned a null value.");

                     long timestamp = currentPoint.Timestamp.UtcTime.Ticks;

                     Dictionary<string, ITimeSeriesValue> data = new Dictionary<string, ITimeSeriesValue>();
                     data.Add(m_settings.PITag, new AdaptValue(m_settings.PITag, Convert.ToDouble(currentPoint.Value), timestamp));

                     IFrame frame = new Frame()
                     {
                         Published = true,
                         Timestamp = timestamp,
                         Measurements = new ConcurrentDictionary<string, ITimeSeriesValue>(data)
                     };

                     yield return frame;
                 }


                 yield break;
             }*/

            yield break;
        }

      

        public IEnumerable<AdaptDevice> GetDevices()
        {
            return new List<AdaptDevice>() { new AdaptDevice(m_settings.InstanceName, "pmu") };
        }

        /// <summary>
        /// Progress Reports are not supported from the OH Data Source
        /// </summary>
        /// <returns> This call Will Fail. </returns>
        public double GetProgress()
        {
            throw new NotImplementedException();
        }

        public Type GetSettingType()
        {
            return typeof(PIHistorianSettings);
        }

        /// <summary>
        /// The <see cref="IDataSource.GetSignals"/> for the PI data source
        /// </summary>
        /// <returns> A List of all available Signals in the Pi Instance.</returns>
        public IEnumerable<AdaptSignal> GetSignals()
        {
            return new List<AdaptSignal>() { new AdaptSignal(m_settings.PITag, "Signal", m_settings.InstanceName)
            { 
                FramesPerSecond = 30.0,
                Phase = Phase.NONE,
                Type = MeasurementType.Frequency
            }};
        }

        /// <summary>
        /// The PI DataSource does not support Progress Reports.
        /// </summary>
        /// <returns>Returns False.</returns>
        public bool SupportProgress()
        {
            return false;
        }

        /// <summary>
        /// The Test Function required by <see cref="IDataSource.Test"/>
        /// </summary>
        /// <returns> A boolean indicating if it was able to connect to the PI and the requested Instance exists.</returns>
        public bool Test()
        {
            InitializeHost();

            try
            {
                ConnectPI();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void ConnectPI()
        {
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
        private void InitializeHost()
        {
            
            if (AFSDKHost == null)
            {
                string hostApp = Path.GetFullPath($@".\AFSDKhost\AFSDKhost.exe");

                if (!File.Exists(hostApp))
                    throw new InvalidOperationException($"Failed to find \"AFSDKhost.exe\" build for testing, check path: {hostApp}");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WindowStyle =  ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = hostApp
                };

                AFSDKHost = Process.Start(startInfo);
            }

            // Make sure .NET core AFSDK API is initialized
            API.Initialize("localhost");
        }

        #endregion

        #region [ static ]

        private static Process AFSDKHost = null;
        #endregion
    }
}
