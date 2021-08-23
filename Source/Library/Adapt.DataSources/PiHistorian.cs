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
using GemstoneCommon;
using GemstonePhasorProtocolls;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OSIsoft.AF.Asset;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
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
    /// Represents a data source adapter that imports data from a OSISoft PI Instance.
    /// </summary>
    [Description("OSISoft PI Historian: Imports Phasor data from a PI Instance.")]
    public class PIHistorian : IDataSource
    {
        

        #region [ Members ]

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
            }
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
           try
            {
                using (PIConnection connection = new PIConnection
                {
                    ServerName = m_settings.ServerName,
                    UserName = null, //m_settings.UserName,
                    Password = null, //m_settings.Password,
                    ConnectTimeout = m_settings.ConnectTimeout
                })
                {

                    connection.Open();
                    return PIPoint.TryFindPIPoint(connection.Server, m_settings.PITag, out PIPoint point);
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

     

        #endregion

        #region [ static ]

        #endregion
    }
}
