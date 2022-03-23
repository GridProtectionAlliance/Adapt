//******************************************************************************************************
//  Polynomial.cs - Gbtc
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
    /// Represents a collection of Polynomial related functions
    /// </summary>
    internal static class Polynomial
    {
        /// <summary>
        /// Evaluate a Polynomial
        /// </summary>
        /// <param name="x">x value</param>
        /// <param name="coef">Coefficents</param>
        /// <param name="n">Order of the Polynomial</param>
        /// <returns></returns>
        public static double Evaluate(double x, double[] coef, int n)
        {
            double ans;

            ans = coef[0];

            for (int i = 1; i <= n; i++)
                ans = ans * x + coef[i];

            return ans;
        }

        /// <summary>
        /// Evaluate a Polynomial
        /// </summary>
        /// <param name="x">x value</param>
        /// <param name="coef">Coefficents</param>
        /// <returns></returns>
        public static double Evaluate(double x, double[] coef) => Evaluate(x, coef, coef.Length - 1);

        /// <summary>
        /// Evaluate a standard Polynomial where a[n] = 1
        /// </summary>
        /// <param name="x">x value</param>
        /// <param name="coef">Coefficents</param>
        /// <param name="n">Order of the Polynomial</param>
        /// <returns></returns>
        public static double EvaluateSpecial(double x, double[] coef, int n)
        {
            double ans;

            ans = x + coef[0];

            for (int i = 1; i <= n; i++)
                ans = ans * x + coef[i];

            return ans;
        }
    }
}

