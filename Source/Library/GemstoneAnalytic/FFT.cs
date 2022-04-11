//******************************************************************************************************
//  FFT.cs - Gbtc
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


    /// <summary>
    /// Computes the FFT of a signal
    /// </summary>
    public class FFT
    {
        #region[ Properties ]

        public Complex[] ComplexMagnitude { get; }
        public double[] Magnitude => ComplexMagnitude.Select(item => item.Magnitude).ToArray();

        public double[] Phase => ComplexMagnitude.Select(item => item.Phase).ToArray();

        public double[] Frequency { get; }
        public double FrequencyBinWidth { get; }

        #endregion[ Properties ]


        #region[ Constructor ]

        public FFT( double[] data)
        {
            int n = data.Length;
            int m = n;
            ComplexMagnitude = new Complex[n];
            Frequency = new double[n];

            double[] result = new double[m];
            double pi_div = 2.0 * Math.PI / n;
            FrequencyBinWidth = pi_div;

            for (int w = 0; w < m; w++)
            {
                double a = w * pi_div;
                Frequency[w] = a;
                for (int t = 0; t < n; t++)
                {
                    ComplexMagnitude[w] += data[t] * new Complex(Math.Cos(a * t), Math.Sin(a * t)) ;
                }
            }
        }



        #endregion [ Constructor ]

    }
}
