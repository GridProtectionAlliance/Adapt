//******************************************************************************************************
//  WindowingFunctions.cs - Gbtc
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
    /// Helper Function that creates various Windowing Functions
    /// </summary>
    public static class WindowingFunctions
    {
       

        #region[ Constructors ]

        public static double[] Create(WindowFunction Type, int Length)
        {
            switch (Type)
            {
                case (WindowFunction.rectwin):
                    return GenerateRectWin(Length);
                case (WindowFunction.hann):
                    return GenerateHann(Length);
            }

            return new double[Length];
        }

        public static double GetPower(WindowFunction Type, int Length)
        {
            return Create(Type, Length).Sum(v => v * v); 
        }



        #endregion [ Constructors ]

        #region [ Static ]
        private static double[] GenerateRectWin(int length)
        {
            double[] window = new double[length];

            Array.Fill<double>(window, 1.0);

            return window;
        }

        private static double[] GenerateHann(int length)
        {
            double[] window = new double[length];
            for (int i =0; i < length; i++)
                window[i] = Math.Sin(Math.PI * i / length) * Math.Sin(Math.PI * i / length);
            return window;
        }

        #endregion [ Static ]
    }
}
