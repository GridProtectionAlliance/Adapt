// ******************************************************************************************************
//  MultiSignalBaseAnalytic.tsx - Gbtc
//
//  Copyright © 2022, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  03/30/2022 - C. Lackner
//       Generated original version of source code.
// ******************************************************************************************************

using GemstoneCommon;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adapt.DataSources
{
    /// <summary>
    /// Abstract class that implements a number of standard functions for Analytics
    /// </summary>
    public abstract class MultiSignalBaseAnalytic: BaseAnalytic
    {
        
        public override void SetInputFPS(IEnumerable<int> inputFramesPerSeconds)
        {
            m_fps = inputFramesPerSeconds.Aggregate((S, val) => S * val / GetGCD(S, val));
        }
        private int GetGCD(int a, int b)
        {
            int remainder;

            while (b != 0)
            {
                remainder = a % b;
                a = b;
                b = remainder;
            }

            return a;
        }
    }
}
