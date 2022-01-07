// ******************************************************************************************************
//  PassThrough.tsx - Gbtc
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
//       Changed to a Nominal Frequency Analytic
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
    /// if the frequency value/base frequency is between the max and min values, return frequency value. Otherwise return NaN
    /// </summary>
    
    [AnalyticSection(AnalyticSection.DataCleanup)]
    [Description("Nominal Frequency: Returns frequency if frequency / baseFrequency is between max and min.")]
    public class NominalFrequency: IAnalytic
    {
        private Setting m_settings;
        private int m_fps;
        public class Setting
        {
            public double BaseFrequency { get; set; }
            public double Max { get; set; }
            public double Min { get; set; }
        }

        public Type SettingType => typeof(Setting);

        public int FramesPerSecond => m_fps;

        int IAnalytic.PrevFrames => 0;

        int IAnalytic.FutureFrames => 0;

        public IEnumerable<string> OutputNames()
        {
            return new List<string>() { "Nominal" };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Frequency" };
        }

        public Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames)
        {
            ITimeSeriesValue frequency = frame.Measurements["Frequency"];
            if ((frequency.Value / m_settings.BaseFrequency) < m_settings.Max && (frequency.Value / m_settings.BaseFrequency) > m_settings.Min)
                return Task.FromResult<ITimeSeriesValue[]>(new AdaptValue[] { new AdaptValue("Nominal", frequency.Value, frame.Timestamp) });
            else
                return Task.FromResult<ITimeSeriesValue[]>(new AdaptValue[] { new AdaptValue("Nominal", double.NaN, frame.Timestamp) });
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }

        public void SetInputFPS(IEnumerable<int> inputFramesPerSecond)
        {
            m_fps = inputFramesPerSecond.FirstOrDefault();
            foreach (int i in inputFramesPerSecond)
            {
                if (i > m_fps)
                    m_fps = i;
            }
        }
    }
}
