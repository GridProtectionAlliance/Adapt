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
        public double Min { get; set; }
        public double Max { get; set; }
        public double Avg => (double.IsNaN(Sum)? Sum / (double)N : double.NaN);
    
        public int N { get; set; }

        public double Sum { get; set; }
        public GraphPoint(byte[] data)
        {

        }

        public GraphPoint()
        {
            Min = double.NaN;
            Max = double.NaN;
            Sum = double.NaN;
            N = 0;
        }

        public byte[] ToByte()
        {
            byte[] data = new byte[1+8+4+8+8];

            // File format version 1 is min -> N -> Sum -> max
            data[0] = 0x01;
            BitConverter.GetBytes(Min).CopyTo(data, 1);
            BitConverter.GetBytes(N).CopyTo(data, 9);
            BitConverter.GetBytes(Sum).CopyTo(data, 13);
            BitConverter.GetBytes(Max).CopyTo(data, 13+8);

            return data;
        }


     
    }
}
