// ******************************************************************************************************
//  AdaptSignal.tsx - Gbtc
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
//  04/01/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Gemstone;
using Gemstone.Data;
using GemstoneCommon;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Adapt.Models
{
    /// <summary>
    /// Represents a Signal in adapt
    /// </summary>
    public class AdaptSignal
    {
        #region [ Members ]
        private string m_Key;
        private int m_fps;
        private string m_Name;
        private string m_DeviceKey;
        private Phase m_phase;
        private MeasurementType m_measurmentType;
        private string m_description;
        #endregion

        #region [ Properties ]
        public string ID => m_Key;

        public int FramesPerSecond
        {
            get => m_fps;
            set => m_fps = value;
        }
           
        /// <summary>
        /// The Name of this Signal in user readable form.
        /// </summary>
        public string Name
        {
            get => m_Name;
            set { m_Name = value; }
        }

        public string Device => m_DeviceKey;

        public Phase Phase
        {
            get => m_phase;
            set => m_phase = value;
        }
        public MeasurementType Type
        {
            get => m_measurmentType;
            set => m_measurmentType = value;
        }
        public string Description
        {
            get => m_description;
            set => m_description = value;

        }

        public void ReplaceVars(Tuple<string, string>[] vars)
        {
            string subs = m_DeviceKey;

            // Full Name replacement for ""
            if (vars.Length > 0 && vars[0].Item1.Length == 0)
                subs = vars[0].Item2;

            foreach (Tuple<string, string> var in vars)
                subs = subs.Replace("{" + var.Item1 + "}", var.Item2);

            m_DeviceKey = subs;
        }

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="AdaptSignal"/> attached to a Device.
        /// </summary>
        /// <param name="Name">The Name of the new Signal</param>
        /// <param name="Device">The <see cref="IDevice"/> the signal is attached to</param>
        /// <param name="Key">The Unique Identifier for this Channel.</param>
        public AdaptSignal(string Key, string Name, IDevice Device, int framesPerSecond)
        {
            m_Name = Name;
            m_DeviceKey = Device.ID;
            m_Key = Key;
            m_description = "";
            m_fps = framesPerSecond;
        }

        /// <summary>
        /// Creates a new <see cref="AdaptSignal"/> attached to a Device.
        /// </summary>
        /// <param name="Name">The Name of the new Signal</param>
        /// <param name="DeviceID">The Device ID the Signal is attached to</param>
        /// <param name="Key">The Unique Identifier for this Channel.</param>
        public AdaptSignal(string Key, string Name, string DeviceID, int framesPerSecond)
        {
            m_Name = Name;
            m_DeviceKey = DeviceID;
            m_Key = Key;
            m_description = "";
            m_fps = framesPerSecond;
        }
        #endregion

        #region[ Statics ]

        /// <summary>
        /// Gets a List of all Signals available from a DataSource, including any Metadata adjustments saved to the Database
        /// </summary>
        /// <param name="DataSource"> The <see cref="IDataSource"/></param>
        /// <param name="DataSourceId"> The ID of the <see cref="DataSource"/></param>
        /// <param name="ConnectionString"> The connection string to connect to the database.</param>
        /// <param name="DataProviderString">The Data Provider string for the database.</param>
        /// <returns> An <see cref="IEnumerable{AdaptSignal}"/> with all Signals for the specified DataSource. </returns>
        public static IEnumerable<AdaptSignal> Get(IDataSource DataSource, int DataSourceId, string ConnectionString, string DataProviderString)
        {
            IEnumerable<AdaptSignal> result = DataSource.GetSignals();
            Dictionary<string, MeasurementType> CustomSignalTypes;
            Dictionary<string, Phase> CustomSignalPhases;
            Dictionary<string, string> CustomSignalNames;

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            {
                DataTable TypeTbl = connection.RetrieveData("SELECT SignalID, Value FROM SignalMetaData WHERE DataSourceID={0} AND Field='Type' ", DataSourceId);
                DataTable PhaseTbl = connection.RetrieveData("SELECT SignalID, Value FROM SignalMetaData WHERE DataSourceID={0} AND Field='Phase' ", DataSourceId);
                DataTable SignalNameTbl = connection.RetrieveData("SELECT SignalID, Value FROM SignalMetaData WHERE DataSourceID={0} AND Field='Name' ", DataSourceId);

                CustomSignalTypes = TypeTbl.Select().ToDictionary(r => r["SignalID"].ToString(), r => Enum.Parse<MeasurementType>(r["Value"].ToString()));
                CustomSignalPhases = PhaseTbl.Select().ToDictionary(r => r["SignalID"].ToString(), r => Enum.Parse<Phase>(r["Value"].ToString()));
                CustomSignalNames = SignalNameTbl.Select().ToDictionary(r => r["SignalID"].ToString(), r => r["Value"].ToString());
               
            }

            result = result.Select(signal =>
            {
                if (CustomSignalTypes.ContainsKey(signal.ID))
                    signal.Type = CustomSignalTypes[signal.ID];

                if (CustomSignalPhases.ContainsKey(signal.ID))
                    signal.Phase = CustomSignalPhases[signal.ID];

                if (CustomSignalNames.ContainsKey(signal.ID))
                    signal.Name = CustomSignalNames[signal.ID];
                return signal;
            });

            return result;

        }
        
        #endregion
    }
}
