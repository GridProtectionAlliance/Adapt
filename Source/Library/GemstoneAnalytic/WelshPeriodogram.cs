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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GemstoneAnalytic
{


    /// <summary>
    /// A WelshPeriodoGramm that uses FFT
    /// </summary>
    public class WelshPeriodoGramm
    {
        #region[ Properties ]

        public double[] Magnitude => ComplexMagnitude.Select(item => item.Magnitude).ToArray();
        public Complex[] ComplexMagnitude { get; }

        public double[] Frequency { get; }

        #endregion[ Properties ]


        #region[ Constructors ]

        public WelshPeriodoGramm(double[] signal1, double[] signal2, double[] windowFunction, int windowOverlap, int medianFilterOrder = 0)
        {
            int dataLength = signal1.Count(); 
            int windowLength = windowFunction.Length;
            int nWindows = 1 + (int)Math.Floor((double)(dataLength - windowLength) / (double)(windowLength - windowOverlap));

            double windowSum = windowFunction.Sum(x => x * x);

            IEnumerable<Complex> P = new List<Complex>();
            for (int i = 0; i < nWindows; i++)
            {
                int start = i * (windowLength - windowOverlap);
                FFT fft1 = new FFT(signal1.Skip(start).Take(windowLength).ToArray());
                FFT fft2 = new FFT(signal2.Skip(start).Take(windowLength).ToArray());
                if (i == 0)
                {
                    Frequency = fft1.Frequency;
                    P = fft1.ComplexMagnitude.Select(p => p*0.0D).ToList();
                }
                if (medianFilterOrder > 0)
                {
                    double Q = 0.0D;
                    for (double j = (medianFilterOrder + 1) / 2; j <= medianFilterOrder; j += 1.0D)
                        Q += 1.0D / j;
                    Complex[] agg = fft1.ComplexMagnitude.Select((v,i) => v * Complex.Conjugate(fft2.ComplexMagnitude[i])).ToArray();
                    double[] medFFT = ApplyMedianFilter(medianFilterOrder, agg.Select(v => v.Magnitude).ToArray());

                    P = P.Select((item, index) => item + (1.0D / (windowSum * (double)nWindows) * Q) * Complex.FromPolarCoordinates(medFFT[index], agg[index].Phase));
                }
                else
                    P = P.Select((item, index) => item + (1.0D / (windowSum * nWindows)) * fft1.ComplexMagnitude[index] * Complex.Conjugate(fft2.ComplexMagnitude[index]));
            }

            ComplexMagnitude = P.ToArray();
        }

        public WelshPeriodoGramm(double[] data, double[] windowFunction, int windowOverlap, int medianFilterOrder=0 )
        {
            int dataLength = data.Count(); // 
            int windowLength = windowFunction.Length;
            int nWindows = 1 + (int)Math.Floor((double)(dataLength - windowLength) / (double)(windowLength - windowOverlap));

            double windowSum = windowFunction.Sum(x => x * x);

            IEnumerable<Complex> P = new List<Complex>();
            for (int i = 0; i < nWindows; i++)
            {
                int start = i* (windowLength - windowOverlap);
                FFT fft = new FFT(data.Skip(start).Take(windowLength).ToArray());
                if (i == 0)
                {
                    Frequency = fft.Frequency;
                    P = fft.ComplexMagnitude.Select(p => p*0.0D).ToList();
                }
                if (medianFilterOrder > 0)
                {
                    double Q = 0.0D;
                    for (double j = (medianFilterOrder + 1) / 2; j <= medianFilterOrder; j += 1.0D)
                        Q += 1.0D / j;
                    double[] medFFT = ApplyMedianFilter(medianFilterOrder, fft.Magnitude);
                    P = P.Select((item, index) => item + (1.0D / (windowSum * (double)nWindows)* Q) * medFFT[index] * medFFT[index]);
                }
                else
                    P = P.Select((item, index) => item + (1.0D / (windowSum * nWindows)) * fft.ComplexMagnitude[index] * Complex.Conjugate(fft.ComplexMagnitude[index]));
            }

            ComplexMagnitude = P.ToArray();
        }

        public WelshPeriodoGramm(double[] data, WindowFunction fx, int windowLength, int windowOverlap,int medianFilterOrder= 0) : this(data, WindowingFunctions.Create(fx, windowLength), windowOverlap, medianFilterOrder)
        {}

        public WelshPeriodoGramm(double[] signal1, double[] signal2, WindowFunction fx, int windowLength, int windowOverlap, int medianFilterOrder = 0) : this(signal1,signal2, WindowingFunctions.Create(fx, windowLength), windowOverlap, medianFilterOrder)
        { }

        #endregion [ Constructors ]

        #region [ Static ]
        private static double[] ApplyMedianFilter(int order, double[] data)
        {
            double[] result = new double[data.Count()];
            int diffStart = order / 2;
            int diffEnd = order / 2 - 1;

            if (order % 2 == 1)
            {
                diffStart = (order-1) / 2;
                diffEnd = (order-1) / 2;
            }


            for (int i= 0; i < data.Count(); i++)
            {
                int start = i - diffStart;
                int end = i + diffEnd;

                if (start < 0)
                    start = 0;
                if (end >= data.Count())
                    end = data.Count() - 1;

                double[] d = data.Skip(start).Take(end - start).OrderBy(v => v).ToArray();

                if (d.Count() == 1)
                    result[i] = d[0];
                else if (d.Count() % 2 == 1)
                    result[i] = (d[(d.Count() - 1) / 2] + d[(d.Count() + 1) / 2]) * 0.5;
                else
                    result[i] = d[d.Count() / 2];
            }

            return result;
        }

        #endregion [ Static ]
    }
}
