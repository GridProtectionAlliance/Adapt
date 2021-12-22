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
//  12/02/2021 - A. Hagemeyer
//       Changed to addition analytic
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
    /// Adds together two different signals
    /// </summary>

    [AnalyticSection(AnalyticSection.DataCleanup)]

    [Description("Addition: Adding two signals together")]
    public class Addition : IAnalytic
    {
        private Setting m_settings;
        private int m_fps;

        public Type SettingType => typeof(Setting);

        public int FramesPerSecond => m_fps;

        public class Setting
        {
            public string TestString { get; }
        }
        
        public IEnumerable<string> OutputNames()
        {
            return new List<string>() { "Addition" };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Signal 1", "Signal 2"};
        }

        public Task<ITimeSeriesValue[]> Run(IFrame frame)
        {
            ITimeSeriesValue signal1 = frame.Measurements["Signal 1"];
            ITimeSeriesValue signal2 = frame.Measurements["Signal 2"];
            AdaptValue result = new AdaptValue("Addition", signal1.Value + signal2.Value, frame.Timestamp);
            return Task.FromResult<ITimeSeriesValue[]>(new AdaptValue[] { result });
        }



        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }

        public void SetInputFPS(IEnumerable<int> inputFramesPerSeconds)
        {
            // # ToDo: Set m_fps to the largest common multiplier. E.g. if inputFrame = [60,30] set m_fps = 30;
            m_fps = inputFramesPerSeconds.FirstOrDefault();
        }
    }
}
