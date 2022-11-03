// ******************************************************************************************************
//  NominalFrequencyAnalytic.tsx - Gbtc
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
    [Description("Limits: Returns value if it is between max and min. Otherwise the value is removed.")]
    public class NominalFrequency: BaseAnalytic, IAnalytic
    {
        private Setting m_settings;
        public class Setting
        {
            [DefaultValue(2)]
            [SettingName("Upper Limit")]
            public double Max { get; set; }

            [DefaultValue(1)]
            [SettingName("Lower Limit")]
            public double Min { get; set; }
        }

        public Type SettingType => typeof(Setting);

        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "Filtered", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Frequency }
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Original" };
        }


        public override ITimeSeriesValue[] Compute(IFrame frame, IFrame[] prev, IFrame[] future) 
        {
            ITimeSeriesValue frequency = frame.Measurements["Original"];
            double v = frequency.Value;
            if (v < m_settings.Max && v > m_settings.Min)
                return new AdaptValue[] { new AdaptValue("Filtered", v, frame.Timestamp) };
           
            return new AdaptValue[] {};
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }

    }
}
