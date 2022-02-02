// ******************************************************************************************************
//  IAnalytic.tsx - Gbtc
//
//  Copyright © 2021, Grid Protection Alliance.  All Rights Reserved.
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
//  08/02/2020 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using GemstoneCommon;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adapt.Models
{
    /// <summary>
    /// Interface for an ADAPT Analytic
    /// </summary>
    public interface IAnalytic: IAdapter
    {
        /// <summary>
        /// Returns the <see cref="Type"/> of the Settings object that defines all Parameters
        /// </summary>
        public new Type SettingType { get; }

        /// <summary>
        /// Returns a List of strings indicating the names of the Output Signals
        /// </summary>
        public IEnumerable<AnalyticOutputDescriptor> Outputs();

        /// <summary>
        /// Returns a List of strings indicating the names of the Input Signals
        /// </summary>
        public IEnumerable<string> InputNames();

        /// <summary>
        /// The actual Analytic Process
        /// </summary>
        /// <param name="frame">The <see cref="IFrame"/> containing the input Data. </param>
        /// <param name="futureFrames"> The future <see cref="IFrame"/> as determined by <see cref="FutureFrames"/></param>
        /// <param name="previousFrames"> The previous <see cref="IFrame"/> as determined by <see cref="PrevFrames"/></param>
        /// <returns> a <see cref="ITimeSeriesValue[]"/> that contains the results. </returns>
        public Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames);

        /// <summary>
        /// Gets the Current FrameRate of this Adapter
        /// </summary>
        public int FramesPerSecond { get; }

        /// <summary>
        /// Sets the FrameRate of the inputs to this Adapter
        /// </summary>
        /// <param name="inputFramesPerSeconds"></param>
        public void SetInputFPS(IEnumerable<int> inputFramesPerSeconds);

        /// <summary>
        /// Gets the number of previous Frames that need to be send to the Analytic
        /// </summary>
        public int PrevFrames { get; }

        /// <summary>
        /// Gets the number of future Frames that need to be send to the Analytic
        /// </summary>
        public int FutureFrames { get; }

    }
}
