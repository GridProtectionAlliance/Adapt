// ******************************************************************************************************
//  RunningAverageAnalytic.tsx - Gbtc
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
    
    [AnalyticSection(AnalyticSection.DataFiltering)]
    [Description("Running Average: Returns the running average of the last N values.")]
    public class RunningAverage: BaseAnalytic,  IAnalytic
    {
        private Setting m_settings;

        public class Setting
        {
            [SettingName("N")]
            [DefaultValue(5)]
            public int AverageOfLast { get; set; }
        }

        public Type SettingType => typeof(Setting);

        public override int PrevFrames => m_settings.AverageOfLast;

        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "Average", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other } 
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Original" };
        }


        public override ITimeSeriesValue[] Compute(IFrame frame, IFrame[] previousFrames, IFrame[] future) 
        {
            double original = frame.Measurements.First().Value.Value;
            double sum = previousFrames.Select(item => item.Measurements["Original"].Value).Sum(v => double.IsNaN(v)? 0 : v);
            double N = previousFrames.Select(item => item.Measurements["Original"].Value).Sum(v => double.IsNaN(v) ? 0 : 1);
            if (N == m_settings.AverageOfLast)
                return new AdaptValue[] { new AdaptValue("Average", sum / N, frame.Timestamp) };
            return new AdaptValue[] { new AdaptValue("Average", double.NaN, frame.Timestamp) };
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }
        
    }
}
