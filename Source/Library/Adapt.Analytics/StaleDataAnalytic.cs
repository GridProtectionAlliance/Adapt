// ******************************************************************************************************
//  StaleDataAnalytic.tsx - Gbtc
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
//  08/02/2021 - C. Lackner
//       Generated original version of source code.
//  12/16/2021 - A. Hagemeyer
//       Changed to a stale data analytic
//
// ******************************************************************************************************


using Adapt.Models;
using GemstoneCommon;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Adapt.DataSources
{
    /// <summary>
    /// Checks to see if the current input is the same as the previous input. Will return NaN if it is, and the data if it is not.
    /// </summary>
    
    [AnalyticSection(AnalyticSection.DataCleanup)]
    [Description("Stale Data: Will return NaN if current input is the same as the previous input")]
    public class StaleData: IAnalytic
    {
        private Setting m_settings;
        private int m_fps;
        public class Setting { }

        public Type SettingType => typeof(Setting);

        public int FramesPerSecond => m_fps;

        public int PrevFrames => 1;

        public int FutureFrames => 0;

        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "Stale", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other }
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Original" };
        }

        public Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames)
        {
            return Task.FromResult<ITimeSeriesValue[]>(Compute(frame, previousFrames));
        }

        public Task CompleteComputation() 
        {
            return Task.Run(() => { });
        }

        public ITimeSeriesValue[] Compute(IFrame frame, IFrame[] previousFrames) 
        {
            double original = frame.Measurements.First().Value.Value;
            double prevValue = previousFrames.FirstOrDefault()?.Measurements.First().Value.Value ?? double.NaN;

            if (original == prevValue)
                return new AdaptValue[] { new AdaptValue("Stale", double.NaN, frame.Timestamp) };
            return new AdaptValue[] { new AdaptValue("Stale", original, frame.Timestamp) };
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }

        public void SetInputFPS(IEnumerable<int> inputFramesPerSecond)
        {
            m_fps = inputFramesPerSecond.FirstOrDefault();
        }
    }
}
