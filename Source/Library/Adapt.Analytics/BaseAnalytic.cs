// ******************************************************************************************************
//  BaseAnalytic.tsx - Gbtc
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
    public abstract class BaseAnalytic
    {
        protected int m_fps;
        public int FramesPerSecond => m_fps;

        public virtual int PrevFrames => 0;

        public virtual int FutureFrames => 0;

        public virtual Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames)
        {
            return Task.Run(() => Compute(frame, previousFrames, futureFrames));
        }

        public virtual Task<ITimeSeriesValue[]> CompleteComputation(Gemstone.Ticks ticks) 
        {
            return Task.FromResult(new ITimeSeriesValue[0]);
        }

        public virtual ITimeSeriesValue[] Compute(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames) 
        {
            return new ITimeSeriesValue[0];
        }

        public virtual void SetInputFPS(IEnumerable<int> inputFramesPerSeconds)
        {
            m_fps = inputFramesPerSeconds.FirstOrDefault();
        }

    }
}
