// ******************************************************************************************************
//  EventSummary.tsx - Gbtc
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
//  03/24/2022 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Gemstone;
using GemstoneCommon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Adapt.Models
{
    /// <summary>
    /// Represents a point used to aggregate <see cref="ITimeSeriesValue"/> for Events for faster plotting.
    /// </summary>
    public class EventSummary
    {
        #region [ Properties ]
      
        public double Sum { get; set; }
        public int Count { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public bool Continuation { get; set; }
        public DateTime Tmin { get; set; }
        public DateTime Tmax { get; set; }

        #endregion

        #region [ Constructor ]

        public EventSummary()
        {
            Continuation = false;
        }

        /// <summary>
        /// Generates a new <see cref="EventSummary"/> from a Byte array.
        /// </summary>
        /// <param name="data"></param>
        public EventSummary(byte[] data)
        {
            if (data[0] != 0x02)
                throw new Exception("Invalid Summary File Format");

            Continuation = BitConverter.ToBoolean(data, 1);
            Count = BitConverter.ToInt32(data, 2);
            Sum = BitConverter.ToDouble(data, 6);
            Min = BitConverter.ToDouble(data, 14);
            Max = BitConverter.ToDouble(data, 22);

            Tmin = DateTime.FromBinary(BitConverter.ToInt64(data, 22 + 8));
            Tmax = DateTime.FromBinary(BitConverter.ToInt64(data, 30 + 8));
        }

        #endregion

        #region [ Methods ]

        public byte[] ToByte()
        {
            // Version 2 => Continuation (1b) => N, Sum, Min, Max
            byte[] data = new byte[NSize];
            data[0] = 0x02;
            BitConverter.GetBytes(Continuation).CopyTo(data, 1);
            BitConverter.GetBytes(Count).CopyTo(data, 2);
            BitConverter.GetBytes(Sum).CopyTo(data, 2 + 4);
            BitConverter.GetBytes(Min).CopyTo(data, 6 + 8);
            BitConverter.GetBytes(Max).CopyTo(data, 14 + 8);
            BitConverter.GetBytes(Tmin.ToBinary()).CopyTo(data, 22 + 8);
            BitConverter.GetBytes(Tmax.ToBinary()).CopyTo(data, 30 + 8);

            return data;
        }

        #endregion

        #region [ Static ]
        public static int NSize => 2 + 4 + 8 + 8 + 8 + 8 + 8;
        #endregion

    }
}
