//******************************************************************************************************
//  StatisticalDistributions.cs - Gbtc
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
    /// Collection of Statistical Gamma functions
    /// </summary>
    internal static class Gamma 
    {

        private static readonly double[] log_A =
        {
                8.11614167470508450300E-4,
            -5.95061904284301438324E-4,
                7.93650340457716943945E-4,
            -2.77777777730099687205E-3,
                8.33333333333331927722E-2
        };

        private static readonly double[] log_B =
        {
            -1.37825152569120859100E3,
            -3.88016315134637840924E4,
            -3.31612992738871184744E5,
            -1.16237097492762307383E6,
            -1.72173700820839662146E6,
            -8.53555664245765465627E5
        };

        private static readonly double[] log_C =
        {
            -3.51815701436523470549E2,
            -1.70642106651881159223E4,
            -2.20528590553854454839E5,
            -1.13933444367982507207E6,
            -2.53252307177582951285E6,
            -2.01889141433532773231E6
        };
        public static double InverseLowerIncomplete(double a, double y)
        {
            return inverse(a, 1 - y);
        }

        private static double inverse(double a, double y)
        {
            // bound the solution
            double x0 = Double.MaxValue;
            double yl = 0;
            double x1 = 0;
            double yh = 1.0;
            double dithresh = 5.0 * Constants.DoubleEpsilon;

            // approximation to inverse function
            double d = 1.0 / (9.0 * a);
            double yy = (1.0 - d - Normal.Inverse(y) * Math.Sqrt(d));
            double x = a * yy * yy * yy;

            double lgm = Gamma.Log(a);

            for (int i = 0; i < 10; i++)
            {
                if (x > x0 || x < x1)
                    goto ihalve;

                yy = Gamma.UpperIncomplete(a, x);
                if (yy < yl || yy > yh)
                    goto ihalve;

                if (yy < y)
                {
                    x0 = x;
                    yl = yy;
                }
                else
                {
                    x1 = x;
                    yh = yy;
                }

                // compute the derivative of the function at this point
                d = (a - 1.0) * Math.Log(x) - x - lgm;
                if (d < -Constants.LogMax)
                    goto ihalve;
                d = -Math.Exp(d);

                // compute the step to the next approximation of x
                d = (yy - y) / d;
                if (Math.Abs(d / x) < Constants.DoubleEpsilon)
                    return x;
                x = x - d;
            }

            // Resort to interval halving if Newton iteration did not converge. 
            ihalve:

            d = 0.0625;
            if (x0 == Double.MaxValue)
            {
                if (x <= 0.0)
                    x = 1.0;

                while (x0 == Double.MaxValue && !Double.IsNaN(x))
                {
                    x = (1.0 + d) * x;
                    yy = Gamma.UpperIncomplete(a, x);
                    if (yy < y)
                    {
                        x0 = x;
                        yl = yy;
                        break;
                    }
                    d = d + d;
                }
            }

            d = 0.5;
            double dir = 0;

            for (int i = 0; i < 400; i++)
            {
                double t = x1 + d * (x0 - x1);

                if (Double.IsNaN(t))
                    break;

                x = t;
                yy = Gamma.UpperIncomplete(a, x);
                lgm = (x0 - x1) / (x1 + x0);

                if (Math.Abs(lgm) < dithresh)
                    break;

                lgm = (yy - y) / y;

                if (Math.Abs(lgm) < dithresh)
                    break;

                if (x <= 0.0)
                    break;

                if (yy >= y)
                {
                    x1 = x;
                    yh = yy;
                    if (dir < 0)
                    {
                        dir = 0;
                        d = 0.5;
                    }
                    else if (dir > 1)
                        d = 0.5 * d + 0.5;
                    else
                        d = (y - yl) / (yh - yl);
                    dir += 1;
                }
                else
                {
                    x0 = x;
                    yl = yy;
                    if (dir > 0)
                    {
                        dir = 0;
                        d = 0.5;
                    }
                    else if (dir < -1)
                        d = 0.5 * d;
                    else
                        d = (y - yl) / (yh - yl);
                    dir -= 1;
                }
            }

            if (x == 0.0 || Double.IsNaN(x))
                throw new ArithmeticException();

            return x;
        }

        /// <summary>
        ///   Upper incomplete regularized Gamma function Q
        ///   (a.k.a the incomplete complemented Gamma function)
        /// </summary>
        /// 
        /// <remarks>
        ///   This function is equivalent to Q(x) = Γ(s, x) / Γ(s).
        /// </remarks>
        /// 
        public static double UpperIncomplete(double a, double x)
        {
            const double big = 4.503599627370496e15;
            const double biginv = 2.22044604925031308085e-16;
            double ans, ax, c, yc, r, t, y, z;
            double pk, pkm1, pkm2, qk, qkm1, qkm2;

            if (x <= 0 || a <= 0)
                return 1.0;

            if (x < 1.0 || x < a)
                return 1.0 - LowerIncomplete(a, x);

            if (Double.IsPositiveInfinity(x))
                return 0;

            ax = a * Math.Log(x) - x - Log(a);

            if (ax < -Constants.LogMax)
                return 0.0;

            ax = Math.Exp(ax);

            // continued fraction
            y = 1.0 - a;
            z = x + y + 1.0;
            c = 0.0;
            pkm2 = 1.0;
            qkm2 = x;
            pkm1 = x + 1.0;
            qkm1 = z * x;
            ans = pkm1 / qkm1;

            do
            {
                c += 1.0;
                y += 1.0;
                z += 2.0;
                yc = y * c;
                pk = pkm1 * z - pkm2 * yc;
                qk = qkm1 * z - qkm2 * yc;
                if (qk != 0)
                {
                    r = pk / qk;
                    t = Math.Abs((ans - r) / r);
                    ans = r;
                }
                else
                    t = 1.0;

                pkm2 = pkm1;
                pkm1 = pk;
                qkm2 = qkm1;
                qkm1 = qk;
                if (Math.Abs(pk) > big)
                {
                    pkm2 *= biginv;
                    pkm1 *= biginv;
                    qkm2 *= biginv;
                    qkm1 *= biginv;
                }
            } while (t > Constants.DoubleEpsilon);

            return ans * ax;
        }

        /// <summary>
        ///   Lower incomplete regularized gamma function P
        ///   (a.k.a. the incomplete Gamma function).
        /// </summary>
        /// 
        /// <remarks>
        ///   This function is equivalent to P(x) = γ(s, x) / Γ(s).
        /// </remarks>
        /// 
        public static double LowerIncomplete(double a, double x)
        {
            if (a <= 0)
                return 1.0;

            if (x <= 0)
                return 0.0;

            if (x > 1.0 && x > a)
                return 1.0 - UpperIncomplete(a, x);

            double ax = a * Math.Log(x) - x - Log(a);

            if (ax < -Constants.LogMax)
                return 0.0;

            ax = Math.Exp(ax);

            double r = a;
            double c = 1.0;
            double ans = 1.0;

            do
            {
                r += 1.0;
                c *= x / r;
                ans += c;
            } while (c / ans > Constants.DoubleEpsilon);

            return ans * ax / a;
        }

        /// <summary>
        ///   Natural logarithm of the gamma function.
        /// </summary>
        /// 
        public static double Log(double x)
        {
            if (x == 0)
                return Double.PositiveInfinity;

            double p, q, w, z;

            if (x < -34.0)
            {
                q = -x;
                w = Log(q);
                p = Math.Floor(q);

                if (p == q)
                    throw new OverflowException();

                z = q - p;
                if (z > 0.5)
                {
                    p += 1.0;
                    z = p - q;
                }
                z = q * Math.Sin(System.Math.PI * z);

                if (z == 0.0)
                    throw new OverflowException();

                z = Constants.LogPI - Math.Log(z) - w;
                return z;
            }

            if (x < 13.0)
            {
                z = 1.0;
                while (x >= 3.0)
                {
                    x -= 1.0;
                    z *= x;
                }
                while (x < 2.0)
                {
                    if (x == 0.0)
                        throw new OverflowException();

                    z /= x;
                    x += 1.0;
                }

                if (z < 0.0)
                    z = -z;

                if (x == 2.0)
                    return System.Math.Log(z);

                x -= 2.0;

                p = x * Polynomial.Evaluate(x, log_B, 5) /Polynomial.EvaluateSpecial(x, log_C, 6);

                return (Math.Log(z) + p);
            }

            if (x > 2.556348e305)
                throw new OverflowException();

            q = (x - 0.5) * Math.Log(x) - x + 0.91893853320467274178;

            if (x > 1.0e8)
                return (q);

            p = 1.0 / (x * x);

            if (x >= 1000.0)
            {
                q += ((7.9365079365079365079365e-4 * p
                    - 2.7777777777777777777778e-3) * p
                    + 0.0833333333333333333333) / x;
            }
            else
            {
                q += Polynomial.Evaluate(p, log_A, 4) / x;
            }

            return q;
        }

    }


}
