﻿// ******************************************************************************************************
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
    
    [AnalyticSection(AnalyticSection.DataCleanup)]
    [Description("Running Average: Returns the running average of the last specified number of values.")]
    public class RunningAverage: IAnalytic
    {
        private Setting m_settings;
        private int m_fps;
        private double[] values = Array.Empty<double>();

        public class Setting
        {
            public double AverageOfLast { get; set; }
        }

        public Type SettingType => typeof(Setting);

        public int FramesPerSecond => m_fps;

        public int PrevFrames => 0;

        public int FutureFrames => 0;

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
            return Task.FromResult<ITimeSeriesValue[]>( Compute(frame) );
        }

        public ITimeSeriesValue[] Compute(IFrame frame) 
        {
            ITimeSeriesValue original = frame.Measurements["Original"];
            if (values.Length >= m_settings.AverageOfLast)
            {
                values = values.Skip(1).ToArray();
                values = values.Append(original.Value).ToArray();
                return new AdaptValue[] { new AdaptValue("Average", values.Average(), original.Timestamp) };
            }

            else 
            {
                values = values.Append(original.Value).ToArray();
                return new AdaptValue[] { new AdaptValue("Average", values.Average(), original.Timestamp) };
            }
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }

        public void SetInputFPS(IEnumerable<int> inputFramesPerSecond)
        {
            m_fps = inputFramesPerSecond.FirstOrDefault();

        }
    }
}