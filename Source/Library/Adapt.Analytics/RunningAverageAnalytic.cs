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
//  01/07/2022 - A. Hagemeyer
//       Changed to a running average analytic
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
    /// Returns a running average of the data
    /// </summary>
    
    [AnalyticSection(AnalyticSection.DataCleanup)]
    [Description("Running Average: Returns the running average of the data.")]
    public class RunningAverage: IAnalytic
    {
        private Setting m_settings;
        private int m_fps;
        private List<double> values = new List<double>();
        private List<double> last5 = new List<double>();

        public class Setting{}

        public Type SettingType => typeof(Setting);

        public int FramesPerSecond => m_fps;

        int IAnalytic.PrevFrames => 0;

        int IAnalytic.FutureFrames => 0;

        public IEnumerable<string> OutputNames()
        {
            return new List<string>() { "Average" };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Original" };
        }

        public Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames)
        {
            ITimeSeriesValue original = frame.Measurements["Original"];
            values.Add(original.Value);
            for (int i = Math.Max(0, values.Count - 5); i < values.Count; i++)
            {
                last5.Add(values[i]);
            }
            return Task.FromResult<ITimeSeriesValue[]>(new AdaptValue[] { new AdaptValue("Average", last5.Sum() / last5.Count, frame.Timestamp.Value) });
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
