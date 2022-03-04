//******************************************************************************************************
//  Filter.cs - Gbtc
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
    /// Represents a Time Domain Filter
    /// </summary>
    public class Filter 
    {
        #region[ Properties ]

        private Complex[] m_Poles;
        private Complex[] m_Zeros;
        private double m_Gain;

        #endregion[ Properties ]

        #region [ internal Classes ]
        private class DiscreteFilter
        {
            public double Gain { get; set; }
            public Complex[] Poles { get; set; }
            public Complex[] Zeros { get; set; }
        }
        #endregion [ internal Classes ]
        #region[ Methods ]

        /// <summary>
        /// Creates a new <see cref="Filter"/> based on continous design
        /// </summary>
        /// <param name="poles">The Continous Poles</param>
        /// <param name="zeros"> The Continous Zeros</param>
        /// <param name="Gain">The Continous Gain </param>
        public Filter(IEnumerable<Complex> poles, IEnumerable<Complex> zeros, double Gain)
        {
            m_Poles = poles.ToArray();
            m_Zeros = zeros.ToArray();
            m_Gain = Gain;
        }

        /// <summary>
        /// Transforms Continous Poles and zeros into Discrete poles and zeros.
        /// this uses the biLinear transformation/ Tustin Approximation
        /// If necessary Prewarping is supported via fp
        /// </summary>
        /// <param name="fs"> Sampling Frequency </param>
        /// <param name="fp"> pre-warp frequency</param>
        private DiscreteFilter ContinousToDiscrete(double fs, double fp = 0)
        {
            DiscreteFilter result = new DiscreteFilter()
            {
                Gain = 1.0,
                Poles = new Complex[m_Poles.Length - 1],
                Zeros = new Complex[m_Zeros.Length - 1]
            };

            if (m_Zeros.Count() < m_Poles.Count())
                result.Zeros = new Complex[m_Poles.Length-1];
            
           
            // prewarp
            double ws = 2 * fs;

            if (fp > 0.0D)
            {
                fp = 2.0D * Math.PI * fp;
                ws = fp / Math.Tan(fp / fs / 2.0D);
            }

            //pole and zero Transformation
            Complex poleProd = 1.0D;
            Complex zeroProd = 1.0D;

            for (int i=0; i < m_Poles.Length; i++)
            {
                Complex p = m_Poles[i];
                result.Poles[i] = (1.0D + p / ws) / (1.0D - p / ws);
                poleProd = poleProd * (ws - p);
            }
            for (int i = 0; i < m_Zeros.Length; i++)
            {
                Complex z = m_Zeros[i];
                result.Zeros[i] = (1.0D + z / ws) / (1.0D - z / ws);
                zeroProd = zeroProd * (ws - z);
            }


            result.Gain = (m_Gain * zeroProd / poleProd).Real;

            if (m_Zeros.Count() < m_Poles.Count())
            {
                for (int i = m_Zeros.Length; i < m_Poles.Length; i++)
                {
                    result.Zeros[i] = -1.0D;
                }
            }

            return result;
        }

        /// <summary>
        /// Scale a Filter such that Gain at Corner frequency is -3dB
        /// </summary>
        /// <param name="fc"> Corner Frequency</param>
        public void Scale(double fc)
        {
            double wc = 2 * Math.PI * fc;

            m_Poles = m_Poles.Select(p => p * wc).ToArray();
            m_Zeros = m_Zeros.Select(p => p * wc).ToArray();

            if (m_Zeros.Length < m_Poles.Length)
            {
                int n = m_Poles.Length - m_Zeros.Length;
                m_Gain = Math.Pow(wc, (double)n) * m_Gain;
            }
        }

        /*
        public void LP2HP()
        {
            Complex k = 1;
            List<Complex> hPFPoles = new List<Complex>();
            List<Complex> hPFZeros = new List<Complex>();
            foreach (Complex p in this.ContinousPoles)
            {
                k = k * (-1.0D / p);
                hPFPoles.Add(1.0D / p);
            }

            foreach (Complex p in this.ContinousZeros)
            {
                k = k * (-p);
                hPFZeros.Add(1.0D / p);
            }

            if (this.ContinousZeros.Count < this.ContinousPoles.Count)
            {
                int n = this.ContinousPoles.Count - this.ContinousZeros.Count;
                for (int i = 0; i < n; i++)
                {
                    hPFZeros.Add(0.0D);
                }
            }

            this.ContinousPoles = hPFPoles;
            this.ContinousZeros = hPFZeros;
            this.DiscretePoles = new List<Complex>();
            this.DiscreteZeros = new List<Complex>();
        }
        */

        /// <summary>
        /// Turns poles into Polynomial coefficients
        /// </summary>
        /// <param name="poles"></param>
        /// <returns></returns>
        private double[] PolesToPolynomial(Complex[] poles)
        {
            int n = poles.Count();

            if (n == 0)
                return new double[0];

            List<Complex> result = new List<Complex>() {1.0D, -poles[0]};

            for (int i = 1; i < n; i++)
            {
                result.Add(0.0D);
                result = result.Select((v, j) => (j > 0? v - poles[i] : v)).ToList();
            }
           
            
            return result.Select(v => v.Real).ToArray();
        }

        /// <summary>
        /// Runs an evenly sampled signal through the Filter
        /// </summary>
        /// <param name="signal"> f(t) for the signal </param>
        /// <param name="fs">The sampling frequency </param>
        /// <returns></returns>
        public double[] Filt(double[] signal, double fs)
        {
            int n = signal.Count();
            double[] output = new double[n];

            DiscreteFilter filter = ContinousToDiscrete(fs);
           
            double[] a = this.PolesToPolynomial(filter.Poles);
            double[] b = this.PolesToPolynomial(filter.Zeros);
            b = b.Select(z => z * filter.Gain).ToArray();

            int order = a.Count() - 1;
           
            //Forward Filtering
            for (int i = order; i < n; i++)
            {
                output[i] = signal[i] * b[0];
                for (int j = 1; j < (order + 1); j++)
                {
                    output[i] += signal[i - j] * b[j] - output[i - j] * a[j];
                }
                output[i] = output[i] / a[0];
            }
            return output;
        }

        /// <summary>
        /// Runs a single sample through a Filter with initialState
        /// </summary>
        /// <param name="value"> The input value</param>
        /// <param name="fs">The sampling frequency</param>
        /// <param name="initialState">The initial state of the filter </param>
        /// <param name="finalState"> The final State of the Filter</param>
        /// <returns> the value of the filtered signal</returns>
        public double Filt(double value, double fs, FilterState initialState, out FilterState finalState)
        {
            DiscreteFilter filter = ContinousToDiscrete(fs);

            double[] a = this.PolesToPolynomial(filter.Poles);
            double[] b = this.PolesToPolynomial(filter.Zeros);
            b = b.Select(z => z * filter.Gain).ToArray();

            double[] s = initialState.StateValue;

            if (s.Length < (a.Length + b.Length - 2))
            {
                s = new double[s.Length - (a.Length + b.Length -2)];
                Array.Fill(s, 0.0D);
                s = initialState.StateValue.Concat(s).ToArray();
            }
                
            double fx = value * b[0] + b.Select((z,i) => (i > 0? z*s[i] : 0.0D)).Sum() ;
            fx += a.Select((z, i) => (i > 0 ? z * s[i+ b.Length] : 0.0D)).Sum();
            fx = fx / a[0];

            finalState = new FilterState()
            {
                StateValue = new double[] { value }
                    .Concat(initialState.StateValue.Take(b.Length - 1).ToArray())
                    .Concat(new double[] { fx })
                    .Concat(initialState.StateValue.Skip(b.Length).Take(a.Length - 1).ToArray())
                    .ToArray()
            };

            return fx;

        }

        /*
        private double[] reverserFilt(double[] signal)
        {
            int n = signal.Count();
            double[] output = new double[n];

            signal.Reverse();


            double[] a = this.PolesToPolynomial(this.DiscretePoles.ToArray());
            double[] b = this.PolesToPolynomial(this.DiscreteZeros.ToArray());
            b = b.Select(z => z * this.DiscreteGain).ToArray();

            int order = a.Count() - 1;
            //setup first few points for computation
            //for (int i = 0; i < order; i++)
            //{
            //    output[i] = signal[i];
            //}

            //Forward Filtering
            for (int i = order; i < n; i++)
            {
                output[i] = signal[i] * b[0];
                for (int j = 1; j < (order + 1); j++)
                {
                    output[i] += signal[i - j] * b[j] - output[i - j] * a[j];
                }
                output[i] = output[i] / a[0];
            }

            output.Reverse();
            return output;
        }

        public double[] filtfilt(double[] signal, double fs)
        {
            double[] forward = filt(signal, fs);
            return reverserFilt(forward);
        }
        */
        #endregion[methods]
    }
}
