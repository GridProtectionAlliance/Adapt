//******************************************************************************************************
//  TimeAlignment.cs - Gbtc
//
//  Copyright © 2012, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  02/20/2022 - C. Lackner
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Linq;

namespace GemstoneAnalytic
{

 
    /// <summary>
    /// Represents an static class that determines Time alignments for multiple Frames Per Second as appropriate
    /// </summary>
    public static class TimeAlignment 
    {
        /// <summary>
        /// Combine multiple Frames Per Seconds into smallest FrameRate that will Capture all Timestamps....
        /// </summary>
        /// <param name="FramesPerSecond"></param>
        /// <returns></returns>
        public static int Combine(params int[] FramesPerSecond)
        {
            return FramesPerSecond.Aggregate((S, val) => S * val / GetGCD(S, val));
        }

        private static int GetGCD(int a, int b)
        {
            int remainder;

            while (b != 0)
            {
                remainder = a % b;
                a = b;
                b = remainder;
            }

            return a;
        }

    }
}
