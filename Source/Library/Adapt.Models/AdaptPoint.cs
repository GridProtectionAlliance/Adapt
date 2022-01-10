// ******************************************************************************************************
//  Adapt{Point.tsx - Gbtc
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
//  05/11/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Gemstone;
using GemstoneCommon;
using System;

namespace Adapt.Models
{
    /// <summary>
    /// Represents Time Series Value including Min, Max and Avg based on <see cref="AdaptValue"/>
    /// Used for graphing
    /// </summary>
    public class AdaptPoint : AdaptValue
    {
        private double m_Max;
        private double m_Min;
        private Ticks m_time;
        private double m_stdev;
        private int M_Npoints;
        private double m_fps;

        public AdaptPoint(AdaptValue value, double Min, double Max): base(value.ID,value.Value,value.Timestamp)
        {
            m_Max = Max;
            m_Min = Min;
        }

        public AdaptPoint(string guid) : base(guid)
        {}

        public AdaptPoint(string guid, double Value, Ticks Time, double Min, double Max, double FPS) : base(guid,Value,Time)
        {
            m_Max = Max;
            m_Min = Min;
            m_time = 0;
            m_fps = FPS;
            M_Npoints = 0;
            m_stdev = 0;
            
        }

        public AdaptPoint(string Guid, double Sum, double SumSqrd, int NCount, Ticks StartTime, Ticks EndTime, double Min, double Max, double FPS) : base(Guid, Sum/(double)NCount, StartTime + (EndTime - StartTime))
        {
            m_Max = Max;
            m_Min = Min;
            m_time = EndTime - StartTime;
            m_stdev = Math.Sqrt((SumSqrd - 2 * Value * Sum + Value * Value) / NCount);
            M_Npoints = NCount;
            m_fps = FPS;

        }

        /// <summary>
        /// The Maximum Value.
        /// </summary>
        public double Max { get => m_Max; set => m_Max = value; }

        /// <summary>
        /// The Minimum Value.
        /// </summary>
        public double Min { get => m_Min; set => m_Min = value; }

        /// <summary>
        /// Data Availability
        /// </summary>
        public double DataAvailability => (m_fps == 0? 0 : (double)M_Npoints/(m_fps*(double)m_time.ToSeconds()));

        /// <summary>
        /// The Standard Deviation.
        /// </summary>
        public double StandardDeviation => m_stdev;


        public static AdaptPoint operator +(AdaptPoint a, double b) => new AdaptPoint(new AdaptValue(a.ID,a.Value+b,a.Timestamp),a.Min*b,a.Max*b);

        public static AdaptPoint operator +(double a, AdaptPoint b) => b + a;

        public static AdaptPoint operator -(AdaptPoint a, double b) => a+ (-1.0D)*b;

        public static AdaptPoint operator -(double a, AdaptPoint b) => b + (-1.0D)*a;

        public static AdaptPoint operator *(AdaptPoint a, double b) => (b > 0? new AdaptPoint(new AdaptValue(a.ID, a.Value*b, a.Timestamp),a.Max* b, a.Min* b) : new AdaptPoint(new AdaptValue(a.ID, a.Value * b, a.Timestamp), a.Min * b, a.Max * b)) ;

        public static AdaptPoint operator *(double a, AdaptPoint b) => b * a;

    }
}
