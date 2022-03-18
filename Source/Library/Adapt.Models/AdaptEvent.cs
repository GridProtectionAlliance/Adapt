// ******************************************************************************************************
//  AdaptEvent.tsx - Gbtc
//
//  Copyright © 2022, Grid Protection Alliance.  All Rights Reserved.
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
//  02/02/2022 - C. Lackner
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
    /// Represents a TimeSeries Value
    /// </summary>
    public class AdaptEvent : ITimeSeriesValue
    {
        private double m_Value = 1;
        private string m_Guid;
        private Ticks m_Time;
        private List<string> m_parameterNames;

        public double Value
        {
            get => m_Value;
            set
            {
                if (double.IsNaN(value) || value >= 0)
                    m_Value = double.NaN;
                else
                    m_Value = 1;
            }
        }

        public string ID =>  m_Guid;
        

        public Ticks Timestamp { get => m_Time; set => m_Time = value; }

        public double LenghtSeconds { get; set; }

        public double[] Parameters { get; set; }

        public List<string> ParameterNames => m_parameterNames;

        public bool IsEvent => true;
        public AdaptEvent(string guid)
        {
            m_Guid = guid;
            m_Value = 1;
            LenghtSeconds = 0;
        }

        public AdaptEvent(string guid, Ticks Time)
        {
            m_Guid = guid;
            m_Time = Time;
            m_Value = 1;
            LenghtSeconds = 0;
        }

        public AdaptEvent(string guid, Ticks Time, double Length)
        {
            m_Guid = guid;
            m_Time = Time;
            m_Value = 1;
            LenghtSeconds = Length;
        }

        public AdaptEvent(string guid, Ticks Time, double Length, List<string> ParameterNames)
        {
            m_Guid = guid;
            m_Time = Time;
            m_Value = 1;
            LenghtSeconds = Length;
            m_parameterNames = ParameterNames;
        }
    }
}
