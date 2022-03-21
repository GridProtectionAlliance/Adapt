// ******************************************************************************************************
//  SubtractionAnalytic.tsx - Gbtc
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
        private int m_fps;

        public Type SettingType => typeof(Setting);

        public int FramesPerSecond => m_fps;
        public class Setting {}

        public int PrevFrames => 0;

        public int FutureFrames => 0;

        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "Difference", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other } 
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Signal 1", "Signal 2" };
        }

        public Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames)
        {
            return Task.Run(() => Compute(frame));
        }

        public Task CompleteComputation() 
        {
            return Task.Run(() => { });
        }

        public ITimeSeriesValue[] Compute(IFrame frame) 
        {
            ITimeSeriesValue signal1 = frame.Measurements["Signal 1"];
            ITimeSeriesValue signal2 = frame.Measurements["Signal 2"];
            return new AdaptValue[] { new AdaptValue("Difference", signal1.Value - signal2.Value, frame.Timestamp) };
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }

        public int GetGCD(int a, int b)
        {
            int remainder;

            while (b != 0)
            {
                remainder = a % b;
                a = b;
                b = remainder;
            }

            return a;
        }

        public void SetInputFPS(IEnumerable<int> inputFramesPerSecond)
        {
            m_fps = inputFramesPerSecond.Aggregate((S, val) => S * val / GetGCD(S, val));
        }
    }
}
