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
//  12/03/2021 - A. Hagemeyer
//       Changed to Subtraction analytic
//
// ******************************************************************************************************


using Adapt.Models;
using GemstoneCommon;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;

namespace Adapt.DataSources
{
    /// <summary>
    /// Subtracts two signals from each other
    /// </summary>

    [AnalyticSection(AnalyticSection.DataCleanup)]

    [Description("Subtraction: Subtracting Signal 2 from Signal 1")]
    public class Subtraction : IAnalytic
    {
        private Setting m_settings;
        public class Setting
        {
            public string TestString { get; }
        }
        public Type GetSettingType()
        {
            return typeof(Setting);
        }

        public IEnumerable<string> OutputNames()
        {
            return new List<string>() { "Difference" };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Signal 1", "Signal 2" };
        }

        public Task<ITimeSeriesValue[]> Run(IFrame frame)
        {
            ITimeSeriesValue signal1 = frame.Measurements["Signal 1"];
            ITimeSeriesValue signal2 = frame.Measurements["Signal 2"];
            AdaptValue result = new AdaptValue("Difference", signal1.Value - signal2.Value, frame.Timestamp);
            return Task.FromResult<ITimeSeriesValue[]>(new AdaptValue[] { result });
        }



        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }
    }
}
