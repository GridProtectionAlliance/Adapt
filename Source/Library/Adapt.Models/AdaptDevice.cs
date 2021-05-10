// ******************************************************************************************************
//  AdaptDevice.tsx - Gbtc
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

namespace Adapt.Models
{
    /// <summary>
    /// Represents PMU in ADAPT
    /// </summary>
    public class AdaptDevice : IDevice, IEquatable<AdaptDevice>, IEquatable<string>
    {
        private string m_Name;
        private string m_Guid;

        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }


        public string ID => m_Guid;

        /// <summary>
        /// Generates a New <see cref="AdaptDevice"/>
        /// </summary>
        /// <param name="key">The Key uniquely identifying this Device</param>
        public AdaptDevice(string key)
        {
            m_Name = key;
            m_Guid = key;
        }

        /// <summary>
        /// Generates a New <see cref="AdaptDevice"/>
        /// </summary>
        /// <param name="key">The Key uniquely identifying this Device</param
        /// <param name="name">The human readbale name of this Device</param>
        public AdaptDevice(string key, string name)
        {
            m_Name = name;
            m_Guid = key;
        }

        public bool Equals(AdaptDevice other)
        {
            return m_Guid == other.ID;
        }

        public bool Equals(string other)
        {
            return m_Guid == other;
        }

        private static AdaptDevice s_unknown;

        public static AdaptDevice Unknown => s_unknown;
    }
}
