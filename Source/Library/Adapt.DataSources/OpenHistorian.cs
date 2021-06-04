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
    [Description("OpenHistorian: Imports Phasor data from an OpenHistorian.")]
    public class OpenHistorian : IDataSource
    {
        
        #region [ Members ]
        private OpenHistorianSettings m_settings;
        private List<AdaptDevice> m_Devices = null;
        private List<AdaptSignal> m_Signals = null;
        #endregion

        #region [ Constructor ]

        public OpenHistorian()
        {}
        #endregion

        #region [ Methods ]
        public void Configure(IConfiguration config)
        {
            m_settings = new OpenHistorianSettings();
            config.Bind(m_settings);

          
        }

        public async IAsyncEnumerable<IFrame> GetData(List<AdaptSignal> signals, DateTime start, DateTime end)
        {
            yield break;
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

        public Type GetSettingType()
        {
            return typeof(OpenHistorianSettings);
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
        /// The OH DataSource does not support Progress Reports.
        /// </summary>
        /// <returns>Returns False.</returns>
        public bool SupportProgress()
        {
            return false;
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
                m_Devices.Add(new AdaptDevice(device.First()["DeviceID"].ToString(), device.Key));

                foreach (JObject measurement in device)
                {
                    string name = measurement["PointTag"].ToString();
                    if (m_settings.NameField == NamingConvention.SignalReference)
                        name = measurement["SignalReference"].ToString();

                    m_Signals.Add(
                        new AdaptSignal(measurement["SignalID"].ToString(), name, m_Devices.Last())
                        { 
                            FramesPerSecond = int.Parse(measurement["FramesPerSecond"].ToString()),
                            Description = measurement["Description"].ToString(),
                            Phase = GetPhase(measurement["Phase"].ToString()),
                            Type = GetType(measurement["SignalType"].ToString())
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
        private MeasurementType GetType(string type)
        {
            switch (type)
            {
                case "IPHM":
                    return MeasurementType.CurrentMagnitude;
                case "IPHA":
                    return MeasurementType.CurrentPhase;
                case "VPHM":
                    return MeasurementType.VoltageMagnitude;
                case "VPHA":
                    return MeasurementType.VoltagePhase;
                case "ALOG":
                    return MeasurementType.Analog;
                case "FREQ":
                    return MeasurementType.Frequency;
                case "DFDT":
                    return MeasurementType.DeltaFrequency;
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

        #endregion

        #region [ static ]
        
        #endregion
    }
}
