//******************************************************************************************************
//  Constantc.cs - Gbtc
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
    /// Constants for Statistical use
    /// </summary>
    internal static class Constants 
    {
        /// <summary>
        ///   Double-precision machine round-off error.
        /// </summary>
        /// 
        /// <remarks>
        ///   This value is actually different from Double.Epsilon. It
        ///   is defined as 1.11022302462515654042E-16.
        /// </remarks>
        /// 
        public const double DoubleEpsilon = 1.11022302462515654042e-16;

        /// <summary>
        ///   Maximum log on the machine.
        /// </summary>
        /// 
        /// <remarks>
        ///   This constant is defined as 7.09782712893383996732E2.
        /// </remarks>
        /// 
        public const double LogMax = 7.09782712893383996732E2;

        /// <summary>
        ///   Log of number pi: log(pi).
        /// </summary>
        /// 
        /// <remarks>
        ///   This constant has the value 1.14472988584940017414.
        /// </remarks>
        /// 
        public const double LogPI = 1.14472988584940017414;
    }
}
