// ******************************************************************************************************
//  FrequencyExcursionDetection.tsx - Gbtc
//
//  Copyright © 2022, Grid Protection Alliance.  All Rights Reserved.
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
//  02/02/2022 - C. Lackner
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
    /// Analytic to Detect Frequency Excursion
    /// </summary>

    [AnalyticSection(AnalyticSection.EventDetection)]

    [Description("Frequency Excursion: Detects Frequency excursions")]
    public class FrequencyExcursion : IAnalytic
    {
        private Setting m_settings;
        private int m_fps;

        public Type SettingType => typeof(Setting);

        public int FramesPerSecond => m_fps;

        public int PrevFrames => 0;

        public int FutureFrames => 0;

        public class Setting 
        {
            [DisplayName("Upper Threshold")]
            [DefaultValue(65)]
            public double upper { get; set; }

            [DisplayName("Lower Threshold")]
            [DefaultValue(45)]
            public double lower { get; set; }

            [DisplayName("Excursion Type")]
            [DefaultValue(ExcursionType.UpperAndLower)]
            public ExcursionType excursionType { get; set; }

        }

        public enum ExcursionType
        {
            Lower,
            Upper,
            [Description("Upper and Lower")]
            UpperAndLower
        }
        
        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "Upper Limit", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other }, 
                new AnalyticOutputDescriptor() { Name = "Lower Limit", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other }
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Frequency" };
        }

        public Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames)
        {
            return Task.Run(() => Compute(frame) );
        }

        public ITimeSeriesValue[] Compute(IFrame frame) 
        {
            List<AdaptEvent> result = new List<AdaptEvent>();

            if (m_settings.excursionType == ExcursionType.Lower || m_settings.excursionType == ExcursionType.UpperAndLower)
                if (frame.Measurements.First().Value.Value < m_settings.lower)
                    result.Add(new AdaptEvent("Lower Limit", frame.Timestamp));

            if (m_settings.excursionType == ExcursionType.Upper || m_settings.excursionType == ExcursionType.UpperAndLower)
                if (frame.Measurements.First().Value.Value > m_settings.upper)
                    result.Add(new AdaptEvent("Upper Limit", frame.Timestamp));

            return result.ToArray();
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }

       
        public void SetInputFPS(IEnumerable<int> inputFramesPerSeconds)
        {
            m_fps = inputFramesPerSeconds.FirstOrDefault();
        }
    }
}
