//******************************************************************************************************
//  DigitalFilter.cs - Gbtc
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
//  03/02/2022 - C. Lackner
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
    /// Represents a Digital Time Domain Filter
    /// </summary>
    public class DigitalFilter 
    {
        #region[ Properties ]

        private double[] m_A;
        private double[] m_B;
        private double m_gain;
        #endregion[ Properties ]


        #region[ Methods ]

        /// <summary>
        /// Creates a new <see cref="DigitalFilter"/> based on 
        /// a[0] y[k] + a[1] y[k-1].... = b[0] x[k] + b[1] x[k-1]... b[n] x[k-n]
        /// </summary>
        /// <param name="A"> a[0] through a[n]</param>
        /// <param name="B"> b[0] through b[n]</param>
        public DigitalFilter(double[] B, double[] A)
        {
            m_A = A;
            m_B = B;
            m_gain = 1.0;
        }

        /// <summary>
        /// Creates a new <see cref="DigitalFilter"/> based on 
        /// a[0] y[k] + a[1] y[k-1].... = K * (b[0] x[k] + b[1] x[k-1]... b[n] x[k-n])
        /// </summary>
        /// <param name="A"> a[0] through a[n]</param>
        /// <param name="B"> b[0] through b[n]</param>
        /// <param name="K"> The gain of the filter. </param>
        public DigitalFilter(double[] B, double[] A, double K): this(B,A)
        {
            m_gain = K;
        }

        /// <summary>
        /// Runs an evenly sampled signal through the Filter
        /// </summary>
        /// <param name="signal"> f(t) for the signal </param>
        /// <returns></returns>
        public double[] Filt(double[] signal)
        {
            int n = signal.Count();
            double[] output = new double[n];

            FilterState state = new FilterState();

            for (int i = 0; i <n; i++)
            {
                output[i] = Filt(signal[i], state, out state);
            }
            
            return output;
        }

        /// <summary>
        /// Runs a single sample through a Filter with initialState
        /// </summary>
        /// <param name="value"> The input value</param>
        /// <param name="initialState">The initial state of the filter </param>
        /// <param name="finalState"> The final State of the Filter</param>
        /// <returns> the value of the filtered signal</returns>
        public double Filt(double value, FilterState initialState, out FilterState finalState)
        {
            double[] s = initialState.StateValue;

            if (s.Length < (m_A.Length + m_B.Length - 2))
            {
                s = new double[(m_A.Length + m_B.Length -2) - s.Length];
                Array.Fill(s, 0.0D);
                s = initialState.StateValue.Concat(s).ToArray();
            }
                
            double fx = value * m_B[0] + m_B.Select((z,i) => (i > 0? z*s[i-1] : 0.0D)).Sum() ;
            fx += m_A.Select((z, i) => (i > 0 ? z * -s[i+ m_B.Length-2] : 0.0D)).Sum();
            fx = fx / m_A[0];

            finalState = new FilterState()
            {
                StateValue = new double[] { value }
                    .Concat(s.Take(m_B.Length - 2).ToArray())
                    .Concat(new double[] { fx })
                    .Concat(s.Skip(m_B.Length-1).Take(m_A.Length - 2).ToArray())
                    .ToArray()
            };

            return fx*m_gain;

        }

        #endregion[methods]
    }
}
