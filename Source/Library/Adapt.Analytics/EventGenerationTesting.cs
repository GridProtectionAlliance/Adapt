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
    /// A simple operation that just duplicates the input signal.
    /// </summary>
    
    [AnalyticSection(AnalyticSection.EventDetection)]
    [Description("Test EventGeneration: This Analytic Creates a new Event for testing.")]
    public class EventGeneration: BaseAnalytic, IAnalytic
    {
        private Setting m_settings;

        public class Setting
        {
            public int Length { get; set; }
            public int Wait { get; set; }

        }

        public Type SettingType => typeof(Setting);

        private Gemstone.Ticks m_lastTS;
        private int m_counter;

        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() {
                new AnalyticOutputDescriptor() { Name = "Event", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.EventFlag },
                new AnalyticOutputDescriptor() { Name = "Counter", FramesPerSecond = 30, Phase = Phase.NONE, Type = MeasurementType.Other }
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Signal" };
        }

      

        public override ITimeSeriesValue[] Compute(IFrame frame, IFrame[] prev, IFrame[] future)
        {
            List<ITimeSeriesValue> results = new List<ITimeSeriesValue>();
            if (m_lastTS == Gemstone.Ticks.MinValue)
            {
                results.Add(new AdaptEvent("Event", frame.Timestamp, m_settings.Length * Gemstone.Ticks.PerMillisecond,
                    new KeyValuePair<string, double>("Counter", m_counter))
                    );
                m_counter++;
                m_lastTS = frame.Timestamp;
            }

            while ((m_lastTS + (m_settings.Wait + m_settings.Length) * Gemstone.Ticks.PerMillisecond) < frame.Timestamp)
            {
                Gemstone.Ticks Ts = m_lastTS + (m_settings.Wait + m_settings.Length) * Gemstone.Ticks.PerMillisecond;
                results.Add(new AdaptEvent("Event", Ts, m_settings.Length * Gemstone.Ticks.PerMillisecond,
                    new KeyValuePair<string, double>("Counter", m_counter))
               );
                m_counter++;
                m_lastTS = Ts;
            }

            results.Add(new AdaptValue("Counter", m_counter, frame.Timestamp));
            return results.ToArray();

        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
            m_lastTS = Gemstone.Ticks.MinValue;
            m_counter = 0;
        }

    }
}
