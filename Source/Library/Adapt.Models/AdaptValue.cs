// ******************************************************************************************************
//  AdaptValue.tsx - Gbtc
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
    /// Represents a TimeSeries Value
    /// </summary>
    public class AdaptValue : ITimeSeriesValue
    {
        private double m_Value;
        private string m_Guid;
        private Ticks m_Time;

        public double Value { get => m_Value; set => m_Value = value; }

        public string ID =>  m_Guid;

        public bool IsEvent => false;
        public Ticks Timestamp { get => m_Time; set => m_Time = value; }

        public AdaptValue(string guid)
        {
            m_Guid = guid;
        }

        public AdaptValue(string guid, double Value, Ticks Time)
        {
            m_Guid = guid;
            m_Time = Time;
            m_Value = Value;

        }
      
        public static AdaptValue operator +(AdaptValue a, double b) => new AdaptValue(a.ID,a.Value+b,a.Timestamp);

        public static AdaptValue operator +(double a, AdaptValue b) => b + a;

        public static AdaptValue operator -(AdaptValue a, double b) => a+ (-1.0D)*b;

        public static AdaptValue operator -(double a, AdaptValue b) => b + (-1.0D)*a;

        public static AdaptValue operator *(AdaptValue a, double b) => new AdaptValue(a.ID, a.Value * b, a.Timestamp);

        public static AdaptValue operator *(double a, AdaptValue b) => b * a;

    }
}
