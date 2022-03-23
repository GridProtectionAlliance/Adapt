﻿// ******************************************************************************************************
//  RingDownAnalysis.tsx - Gbtc
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

    [Description("RingDown Analysis: Detects Frequency excursions")]
    public class RingDownAnalysis : IAnalytic
    {
        private Setting m_settings;
        private int m_fps;
        private Queue<double> m_RMSmed;
        public Type SettingType => typeof(Setting);

        public int FramesPerSecond => m_fps;

        public int PrevFrames => (m_settings?.RMSLength ?? 10) * FramesPerSecond;

        public int FutureFrames => 0;

        public class Setting 
        {
            [SettingName("Length for RMS calculation (s)")]
            [DefaultValue(15)]
            public int RMSLength { get; set; }

            [SettingName("Median Filter Length (s)")]
            [DefaultValue(120)]
            public int FilterOrder { get; set; }

            [SettingName("Threshold Scale")]
            [DefaultValue(3.0)]
            public double Threshold { get; set; }
        }
        
        public RingDownAnalysis()
        {
            m_RMSmed = new Queue<double>();
        }
        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "Ringdown Event", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.EventFlag }, 
                new AnalyticOutputDescriptor() { Name = "RMS Threshold", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other },
                new AnalyticOutputDescriptor() { Name = "RMS Value", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other }
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Input Signal" };
        }

        public Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames)
        {
            return Task.Run(() => Compute(frame, previousFrames) );
        }

        public ITimeSeriesValue[] Compute(IFrame frame, IFrame[] previousFrames)
        {
            List<ITimeSeriesValue> result = new List<ITimeSeriesValue>();
            double Nrms = previousFrames.Sum(v => (!double.IsNaN(v.Measurements["Input Signal"].Value) ? 0.0D : 1.0D));

            double rms = previousFrames.Sum(v => (!double.IsNaN(v.Measurements["Input Signal"].Value)? 0.0D : v.Measurements[""].Value / Nrms));
            rms = Math.Sqrt(rms);

            m_RMSmed.Enqueue(rms);

            int NfilterOrder = m_settings.FilterOrder * m_fps;
            NfilterOrder = NfilterOrder + 1 - NfilterOrder % 2;

            if (m_RMSmed.Count > NfilterOrder)
                m_RMSmed.Dequeue();

            double threshold = m_settings.Threshold* m_RMSmed.ToArray().OrderBy(o => o).ToList()[((NfilterOrder - 1) / 2)];            // Apply median filter to RMS to establish the threshold

            result.Add(new AdaptValue("RMS Threshold", threshold, frame.Timestamp));
            result.Add(new AdaptValue("RMS Value", rms, frame.Timestamp));

            if (rms > threshold)
                result.Add(new AdaptEvent("Ringdown Event", frame.Timestamp));

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
