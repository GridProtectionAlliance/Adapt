﻿// ******************************************************************************************************
//  SignReversalAnalytic.tsx - Gbtc
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
//  12/06/2021 - A. Hagemeyer
//       Changed to a sign reversal analytic
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
    /// Reverses the signs of the signal
    /// </summary>
    
    [AnalyticSection(AnalyticSection.DataFiltering)]

    [Description("Sign Reversal: Reverse the signs of the signal")]
    public class SignReversalAnalytic: BaseAnalytic, IAnalytic
    {
        private Setting m_settings;

        public Type SettingType => typeof(Setting);

        public class Setting { }


        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() {
                new AnalyticOutputDescriptor() { Name = "Reversed", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other }
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Original" };
        }

        public override ITimeSeriesValue[] Compute(IFrame frame, IFrame[] prev, IFrame[] future) 
        {
            ITimeSeriesValue original = frame.Measurements["Original"];
            return new AdaptValue[] { new AdaptValue("Reversed", original.Value * -1, frame.Timestamp) };
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }

    }
}
