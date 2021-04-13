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
using GemstoneCommon;
using System;
using System.Collections.Generic;

namespace Adapt.Models
{
    /// <summary>
    /// Represents a Signal in adapt
    /// </summary>
    public class AdaptSignal
    {
        #region [ Members ]
        private string m_Key;
        private double m_fps;
        private string m_Name;
        private string m_DeviceKey;
        private Phase m_phase;
        private MeasurementType m_measurmentType;
        private string m_description;
        #endregion

        #region [ Properties ]
        public string ID => m_Key;

        public double FramesPerSecond
        {
            get => m_fps;
            set => m_fps = value;
        }
           

        public string Name => m_Name;

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

        #endregion

        #region [ Constructors ]
        /*public AdaptSignal(string name, IDevice device)
        {
            m_Data = new List<AdaptValue>();
            m_Guid = Guid.NewGuid().ToString();
            m_Name = name;
            m_Device = device;
            m_MetaData = new SignalMetaData()
            {
                Phase = Phase.NONE,
                Description = name,
                MeasurmentType = AdaptmeasurmentType.Other
            };
        }

        public AdaptSignal(string name)
        {
            m_Data = new List<AdaptValue>();
            m_Guid = name;
            m_Name = Guid.NewGuid().ToString();
            m_Device = AdaptDevice.Unknown;
            m_MetaData = new SignalMetaData()
            {
                Phase = Phase.NONE,
                Description = name,
                MeasurmentType = AdaptmeasurmentType.Other
            };
        }

        public AdaptSignal(AdaptSignal original)
        {
            m_Data = new List<AdaptValue>((IEnumerable<AdaptValue>)original.Data);
            m_Guid = Guid.NewGuid().ToString();
            m_Name = original.Name;
            m_Device = original.Device;
            m_MetaData = original.MetaData;
        }
        */

        /// <summary>
        /// Creates a new <see cref="AdaptSignal"/> attached to a Device.
        /// </summary>
        /// <param name="Name">The Name of the new Signal</param>
        /// <param name="Device">The <see cref="IDevice"/> the signal is attached to</param>
        /// <param name="Key">The Unique Identifier for this Channel.</param>
        public AdaptSignal(string Key, string Name, IDevice Device)
        {
            m_Name = Name;
            m_DeviceKey = Device.ID;
            m_Key = Key;
            m_description = "";
        }
        #endregion
    }
}
