// ******************************************************************************************************
//  DivisionAnalytic.tsx - Gbtc
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
//       Changed to division analytic
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
    /// Divides two different signals
    /// </summary>

    [AnalyticSection(AnalyticSection.SignalProcessing)]

    [Description("Division: Dividing two signals")]
    public class Division : IAnalytic
    {
        private Setting m_settings;
        private int m_fps;

        public Type SettingType => typeof(Setting);

        public int FramesPerSecond => m_fps;

        public class Setting  {}

        public int PrevFrames => 0;

        public int FutureFrames => 0;

        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "Division", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other } 
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Numerator", "Denominator"};
        }

        public Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] previousFrames)
        {
            return Task.Run(() => Compute(frame));
        }

        public ITimeSeriesValue[] Compute(IFrame frame) 
        {
            ITimeSeriesValue numerator = frame.Measurements["Numerator"];
            ITimeSeriesValue denominator = frame.Measurements["Denominator"];
            return new AdaptValue[] { new AdaptValue("Division", numerator.Value / denominator.Value, frame.Timestamp) };
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

        public void SetInputFPS(IEnumerable<int> inputFramesPerSeconds)
        {
            m_fps = inputFramesPerSeconds.Aggregate((S, val) => S * val / GetGCD(S, val));
        }
    }
}
