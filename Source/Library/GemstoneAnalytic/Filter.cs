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

        private double m_gain;
        private Complex[] m_zeros;
        private Complex[] m_poles;

        #endregion[ Properties ]


        #region[ Methods ]

        /// <summary>
        /// Creates a new <see cref="Filter"/> based on continous design
        /// </summary>
        /// <param name="poles">The Continous Poles</param>
        /// <param name="zeros"> The Continous Zeros</param>
        /// <param name="Gain">The Continous Gain </param>
        public Filter(Complex[] poles, Complex[] zeros, double Gain)
        {
            m_gain = Gain;
            m_poles = poles;
            m_zeros = zeros;

        }

        /// <summary>
        /// Transforms Continous Poles and zeros into Discrete poles and zeros.
        /// this uses the biLinear transformation/ Tustin Approximation
        /// If necessary Prewarping is supported via fp
        /// </summary>
        /// <param name="fs"> Sampling Frequency </param>
        /// <param name="fp"> pre-warp frequency</param>
        public DigitalFilter ContinousToDiscrete(double fs, double fp = 0)
        {
            double DiscreteGain = 1.0;
            Complex[] DiscretePoles = new Complex[m_poles.Count() - 1];
            Complex[] DiscreteZeros = new Complex[m_zeros.Count() - 1];

            if (m_zeros.Count() < m_poles.Count())
                DiscreteZeros = new Complex[m_poles.Count()-1];
            
           
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

            for (int i=0; i < m_poles.Count(); i++)
            {
                Complex p = m_poles[i];
                DiscretePoles[i] = (1.0D + p / ws) / (1.0D - p / ws);
                poleProd = poleProd * (ws - p);
            }
            for (int i = 0; i < m_zeros.Length; i++)
            {
                Complex z = m_zeros[i];
                DiscreteZeros[i] = (1.0D + z / ws) / (1.0D - z / ws);
                zeroProd = zeroProd * (ws - z);
            }


            DiscreteGain = (m_gain * zeroProd / poleProd).Real;

            if (m_zeros.Count() < m_poles.Count())
            {
                for (int i = m_zeros.Length; i < m_poles.Length; i++)
                {
                    DiscreteZeros[i] = -1.0D;
                }
            }

            double[] A = PolesToPolynomial(DiscreteZeros);
            double[] B = PolesToPolynomial(DiscretePoles);
            return new DigitalFilter(B,A,DiscreteGain);
        }

        /// <summary>
        /// Scale a Filter such that Gain at Corner frequency is -3dB
        /// </summary>
        /// <param name="fc"> Corner Frequency</param>
        public void Scale(double fc)
        {
            double wc = 2 * Math.PI * fc;

            m_poles = m_poles.Select(p => p * wc).ToArray();
            m_zeros = m_zeros.Select(p => p * wc).ToArray();

            if (m_zeros.Length < m_poles.Length)
            {
                int n = m_poles.Length - m_zeros.Length;
                m_gain = Math.Pow(wc, (double)n) * m_gain;
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
        /// Turns an existing LPFG into an HPF
        /// </summary>
        private void LP2HP()
        {
            Complex k = 1;
            List<Complex> hPFPoles = new List<Complex>();
            List<Complex> hPFZeros = new List<Complex>();
            foreach (Complex p in m_poles)
            {
                k = k * (-1.0D / p);
                hPFPoles.Add(1.0D / p);
            }

            foreach (Complex p in m_zeros)
            {
                k = k * (-p);
                hPFZeros.Add(1.0D / p);
            }

            if (m_zeros.Count() < m_poles.Count())
            {
                int n = m_poles.Count() - m_zeros.Count();
                for (int i = 0; i < n; i++)
                {
                    hPFZeros.Add(0.0D);
                }
            }

            m_poles = hPFPoles.ToArray();
            m_zeros = hPFZeros.ToArray();

        }

        #endregion [ methods ]

        #region [ static ]

        /// <summary>
        /// Generates a normal Butterworth Filter of Nth order
        /// </summary>
        /// <param name="order"> order of the Filter</param>
        /// <returns></returns>
        private static Filter NormalButter(int order)
        {
            List<Complex> zeros = new List<Complex>();
            List<Complex> poles = new List<Complex>();

            //Generate poles
            for (int i = 1; i < (order + 1); i++)
            {
                double theta = Math.PI * (2 * i - 1.0D) / (2.0D * (double)order) + Math.PI / 2.0D;
                double re = Math.Cos(theta);
                double im = Math.Sin(theta);

                poles.Add(new Complex(re, im));
            }


            Complex Gain = -poles[0];
            for (int i = 1; i < order; i++)
            {
                Gain = Gain * -poles[i];
            }

            return new Filter(poles.ToArray(), zeros.ToArray(), Gain.Real);
        }

        /// <summary>
        /// Generates a High Pass Butterworth Filter
        /// </summary>
        /// <param name="fc"> corner frequency in Hz</param>
        /// <param name="order"> Order of the Filter </param>
        /// <returns></returns>
        public static Filter HPButterworth(double fc, int order)
        {
            Filter result = NormalButter(order);
            result.LP2HP();
            result.Scale(fc);
            return result;
        }

        /// <summary>
        /// Generates a High Pass Butterworth Filter
        /// </summary>
        /// <param name="fc"> corner frequency in Hz</param>
        /// <param name="order"> Order of the Filter </param>
        /// <returns></returns>
        public static Filter LPButterworth(double fc, int order)
        {
            Filter result = NormalButter(order);
            result.Scale(fc);

            return result;
        }

        #endregion
    }
}
