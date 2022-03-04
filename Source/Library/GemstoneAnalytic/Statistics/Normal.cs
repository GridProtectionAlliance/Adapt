//******************************************************************************************************
//  Normal.cs - Gbtc
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
    /// Represents a collection of statistical Normal Distribution as neccesarry
    /// </summary>
    internal static class Normal
    {

        #region [ Constants ]

        private static readonly double[] inverse_P0 =
        {
            -59.963350101410789,
            98.001075418599967,
            -56.676285746907027,
            13.931260938727968,
            -1.2391658386738125
        };

        private static readonly double[] inverse_Q0 =
        {
            1.9544885833814176,
            4.6762791289888153,
            86.360242139089053,
            -225.46268785411937,
            200.26021238006066,
            -82.037225616833339,
            15.90562251262117,
            -1.1833162112133
        };

        private static readonly double[] inverse_P1 =
        {
            4.0554489230596245,
            31.525109459989388,
            57.162819224642128,
            44.080507389320083,
            14.684956192885803,
            2.1866330685079025,
            -0.14025607917135449,
            -0.035042462682784818,
            -0.00085745678515468545
        };

        private static readonly double[] inverse_Q1 =
        {
            15.779988325646675,
            45.390763512887922,
            41.317203825467203,
            15.04253856929075,
            2.5046494620830941,
            -0.14218292285478779,
            -0.038080640769157827,
            -0.00093325948089545744
        };

        private static readonly double[] inverse_P2 =
        {
            3.2377489177694603,
            6.9152288906898418,
            3.9388102529247444,
            1.3330346081580755,
            0.20148538954917908,
            0.012371663481782003,
            0.00030158155350823543,
            2.6580697468673755E-06,
            6.2397453918498331E-09
        };

        private static readonly double[] inverse_Q2 =
        {
            6.02427039364742,
            3.6798356385616087,
            1.3770209948908132,
            0.21623699359449663,
            0.013420400608854318,
            0.00032801446468212774,
            2.8924786474538068E-06,
            6.7901940800998127E-09
        };
        #endregion
        /// <summary>
        ///    Normal (Gaussian) inverse cumulative distribution function.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        ///    For small arguments <c>0 &lt; y &lt; exp(-2)</c>, the program computes <c>z =
        ///    sqrt( -2.0 * log(y) )</c>;  then the approximation is <c>x = z - log(z)/z  - 
        ///    (1/z) P(1/z) / Q(1/z)</c>.</para>
        /// <para>
        ///    There are two rational functions P/Q, one for <c>0 &lt; y &lt; exp(-32)</c> and
        ///    the other for <c>y</c> up to <c>exp(-2)</c>. For larger arguments, <c>w = y - 0.5</c>,
        ///    and  <c>x/sqrt(2pi) = w + w^3 * R(w^2)/S(w^2))</c>.</para>
        /// </remarks>
        /// 
        /// <returns>
        ///    Returns the value, <c>x</c>, for which the area under the Normal (Gaussian)
        ///    probability density function (integrated from minus infinity to <c>x</c>) is
        ///    equal to the argument <c>y</c> (assumes mean is zero, variance is one).
        /// </returns>
        /// 
        public static double Inverse(double y0)
        {
            if (y0 <= 0.0)
            {
                if (y0 == 0)
                    return Double.NegativeInfinity;
                throw new ArgumentOutOfRangeException("y0");
            }

            if (y0 >= 1.0)
            {
                if (y0 == 1)
                    return Double.PositiveInfinity;
                throw new ArgumentOutOfRangeException("y0");
            }


            double s2pi = Math.Sqrt(2.0 * Math.PI);
            int code = 1;
            double y = y0;
            double x;



            if (y > 0.8646647167633873)
            {
                y = 1.0 - y;
                code = 0;
            }

            if (y > 0.1353352832366127)
            {
                y -= 0.5;
                double y2 = y * y;
                x = y + y * ((y2 * Polynomial.Evaluate(y2, inverse_P0, 4)) / Polynomial.EvaluateSpecial(y2, inverse_Q0, 8));
                x *= s2pi;
                return x;
            }

            x = Math.Sqrt(-2.0 * Math.Log(y));
            double x0 = x - Math.Log(x) / x;
            double z = 1.0 / x;
            double x1;

            if (x < 8.0)
            {
                x1 = (z * Polynomial.Evaluate(z, inverse_P1, 8)) / Polynomial.EvaluateSpecial(z, inverse_Q1, 8);
            }
            else
            {
                x1 = (z * Polynomial.Evaluate(z, inverse_P2, 8)) / Polynomial.EvaluateSpecial(z, inverse_Q2, 8);
            }

            x = x0 - x1;

            if (code != 0)
                x = -x;

            return x;
        }
    }
}
