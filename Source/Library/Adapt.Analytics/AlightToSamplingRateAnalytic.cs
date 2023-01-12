// ******************************************************************************************************
//  AlignToSamplingRate.tsx - Gbtc
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
//  11/12/2022 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Adapt.Models;
using GemstoneCommon;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Adapt.DataSources
{
    /// <summary>
    /// Checks to see if the current input is the same as the previous input. Will return NaN if it is, and the data if it is not.
    /// </summary>
    
    [AnalyticSection(AnalyticSection.DataCleanup)]
    [Description("Align Data: Will align the Timestamp of the signal to it's sampling rate to fix Timestamp errors")]
    public class AlignToSamplingRate: BaseAnalytic, IAnalytic
    {
        private Setting m_settings;
        public class Setting { }

        public Type SettingType => typeof(Setting);

        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "Aligned", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other }
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Signal" };
        }

        public override ITimeSeriesValue[] Compute(IFrame frame, IFrame[] previousFrames, IFrame[] future) 
        {
            double original = frame.Measurements["Original"].Value;
            return new AdaptValue[] { new AdaptValue("Aligned", original, frame.Timestamp) };
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }

    }
}
