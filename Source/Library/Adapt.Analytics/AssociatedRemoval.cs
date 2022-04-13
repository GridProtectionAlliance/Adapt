// ******************************************************************************************************
//  AssociatedRemoval.tsx - Gbtc
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
//       Changed to a zeros analytic
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
    /// If value = 0 return NaN. Otherwise return the data
    /// </summary>
    
    [AnalyticSection(AnalyticSection.DataCleanup)]
    [Description("Bad Data Combination: Removes values if either of the signals has no Value.")]
    public class AssociatedRemoval : BaseAnalytic, IAnalytic
    {
        private Setting m_settings;
        public class Setting { }

        public Type SettingType => typeof(Setting);


        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "Cleaned 1", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other },
                new AnalyticOutputDescriptor() { Name = "Cleaned 2", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other }
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Original 1", "Original 2" };
        }

        public override ITimeSeriesValue[] Compute(IFrame frame, IFrame[] prev, IFrame[] future) 
        {
            ITimeSeriesValue s1 = frame.Measurements["Original 1"];
            ITimeSeriesValue s2 = frame.Measurements["Original 2"];
            if (!double.IsNaN(s1.Value) && !double.IsNaN(s2.Value))
                return new AdaptValue[] { new AdaptValue("Cleaned 1", s1.Value, frame.Timestamp), new AdaptValue("Cleaned 2", s2.Value, frame.Timestamp) };
            else
                return new AdaptValue[] { new AdaptValue("Cleaned 1", double.NaN, frame.Timestamp), new AdaptValue("Cleaned 2", double.NaN, frame.Timestamp) };
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }
    }
}
