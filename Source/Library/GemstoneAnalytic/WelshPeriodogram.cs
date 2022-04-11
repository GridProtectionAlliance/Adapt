//******************************************************************************************************
//  WelshPeriodoGramm.cs - Gbtc
//
//  Copyright © 2022, Grid Protection Alliance.  All Rights Reserved.
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
//  04/11/2022 - C. Lackner
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace GemstoneAnalytic
{

    public enum WindowFunction
    {
        rectwin,
        hann
    }


    /// <summary>
    /// A WelshPeriodoGramm that uses FFT
    /// </summary>
    public class WelshPeriodoGramm
    {
        #region[ Properties ]

        public double[] Power { get; }

        public double[] Frequency { get; }
        #endregion[ Properties ]


        #region[ Constructors ]

        public WelshPeriodoGramm(double[] data, double[] windowFunction, int windowOverlap )
        {
            int dataLength = data.Count(); // 
            int windowLength = windowFunction.Length;
            int nWindows = 1 + (int)Math.Floor((double)(dataLength - windowLength) / (double)(windowLength - windowOverlap));

            double windowSum = windowFunction.Sum(x => x * x);

            IEnumerable<double> P = new List<double>();
            for (int i = 0; i < nWindows; i++)
            {
                int start = i* (windowLength - windowOverlap);
                FFT fft = new FFT(data.Skip(start).Take(windowLength).ToArray());
                if (i == 0)
                    Frequency = fft.Frequency;
                P = P.Select((item, index) => item + (1.0D / (windowSum * nWindows)) * fft.Magnitude[index] * fft.Magnitude[index]);
            }

            Power = P.ToArray();
        }

        public WelshPeriodoGramm(double[] data, WindowFunction fx, int windowLength, int windowOverlap) : this(data, GenerateWindow(fx, windowLength), windowOverlap)
        {}


        #endregion [ Constructors ]

        #region [ Static ]
        private static double[] GenerateWindow(WindowFunction fx, int length)
        {
            double[] window = new double[length];

            if (fx == WindowFunction.rectwin)
                Array.Fill<double>(window, 1.0);

            if (fx == WindowFunction.hann)
                for (int i =0; i < length; i++)
                {
                    window[i] = Math.Sin(Math.PI * i / length) * Math.Sin(Math.PI * i / length);
                }
            
            return window;
        }

        #endregion [ Static ]
    }
}
