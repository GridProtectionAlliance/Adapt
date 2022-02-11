// ******************************************************************************************************
//  GraphPoint.tsx - Gbtc
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
//  04/07/2021 - C. Lackner
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
    /// Represents a point used to aggregate <see cref="ITimeSeriesValue"/> for faster plotting.
    /// </summary>
    public class GraphPoint
    {
        #region [ Properties ]
        public double Min { get; set; }
        public double Max { get; set; }
        public double Avg => (!double.IsNaN(Sum)? Sum / (double)N : double.NaN);
    
        public int N { get; set; }

        public double Sum { get; set; }

        public double SquaredSum { get; set; }

        public DateTime Tmin { get; set; }
        public DateTime Tmax { get; set; }

        #endregion 

        #region [ Constructors ]
        public GraphPoint()
        {
            Min = double.NaN;
            Max = double.NaN;
            Sum = double.NaN;
            SquaredSum = double.NaN;
            Tmin = DateTime.MaxValue;
            Tmax = DateTime.MinValue;
            N = 0;
        }

        /// <summary>
        /// Generates a new <see cref="GraphPoint"/> from a Byte array.
        /// </summary>
        /// <param name="data"></param>
        public GraphPoint(byte[] data)
        {
            if (data[0] != 0x01)
                throw new Exception("Invalid Summary File Format");
            
            Min = BitConverter.ToDouble(data, 1);
            N = BitConverter.ToInt32(data, 9);
            Sum = BitConverter.ToDouble(data, 13);
            Max = BitConverter.ToDouble(data, 13 + 8);
            SquaredSum = BitConverter.ToDouble(data, 21 + 8);

            Tmin = DateTime.FromBinary(BitConverter.ToInt64(data, 21 + 8 + 8));
            Tmax = DateTime.FromBinary(BitConverter.ToInt64(data, 21 + 8 + 8 + 8));
            
        }

        #endregion

        #region [ Methods ]
        public byte[] ToByte()
        {
            byte[] data = new byte[NSize];

            // File format version 2 is min -> N -> Sum -> max -> Squared Sum -> Tmin ->  Tmax
            data[0] = 0x01;
            BitConverter.GetBytes(Min).CopyTo(data, 1);
            BitConverter.GetBytes(N).CopyTo(data, 9);
            BitConverter.GetBytes(Sum).CopyTo(data, 13);
            BitConverter.GetBytes(Max).CopyTo(data, 13+8);
            BitConverter.GetBytes(SquaredSum).CopyTo(data, 21 + 8);
            BitConverter.GetBytes(Tmin.ToBinary()).CopyTo(data, 21 + 8 + 8);
            BitConverter.GetBytes(Tmax.ToBinary()).CopyTo(data, 21 + 8 + 8 + 8);
            return data;
        }

        #endregion

        #region [ static ] 
        public static int NSize => 21 + 8 + 8 + 8 + 8;
        #endregion
    }
}
