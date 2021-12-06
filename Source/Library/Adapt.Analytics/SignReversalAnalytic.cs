﻿// ******************************************************************************************************
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
    
    [AnalyticSection(AnalyticSection.DataCleanup)]

    [Description("Sign Reversal: Reverse the signs of the signal")]
    public class SignReversalAnalytic: IAnalytic
    {
        private Setting m_settings;
        public class Setting
        {
            public double Shift { get; }
        }
        public Type GetSettingType()
        {
            return typeof(Setting);
        }

        public IEnumerable<string> OutputNames()
        {
            return new List<string>() { "Reversed" };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Original" };
        }

        public Task<ITimeSeriesValue[]> Run(IFrame frame)
        {
            return Task.FromResult<ITimeSeriesValue[]>(frame.Measurements.ToList().Select(item => new AdaptValue(item.Value.ID, (item.Value.Value * -1), item.Value.Timestamp)).ToArray());
        }


        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }
    }
}
