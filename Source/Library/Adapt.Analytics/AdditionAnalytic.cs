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
        public class Setting
        {
            public string TestString { get; set; }
        }
        public Type GetSettingType()
        {
            return typeof(Setting);
        }

        public IEnumerable<string> OutputNames()
        {
            return new List<string>() { "Addition" };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Original", "Original2"};
        }

        public Task<ITimeSeriesValue[]> Run(IFrame frame)
        {
            AdaptValue first = new AdaptValue(frame.Measurements.First().Value.ID, frame.Measurements.First().Value.Value, frame.Measurements.First().Value.Timestamp);
            AdaptValue last = new AdaptValue(frame.Measurements.Last().Value.ID, frame.Measurements.Last().Value.Value, frame.Measurements.Last().Value.Timestamp);
            return Task.FromResult<ITimeSeriesValue[]>(frame.Measurements.ToList().Select(item => new AdaptValue(first.ID + last.ID, first.Value + last.Value, item.Value.Timestamp)).ToArray());
        }



        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }
    }
}
