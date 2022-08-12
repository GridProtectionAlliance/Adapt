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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Adapt.DataSources
{
    /// <summary>
    /// Analytic to Detect Frequency Excursion
    /// </summary>

    [AnalyticSection(AnalyticSection.EventDetection)]

    [Description("Excursion Detection: Detects excursions in Frequency or Voltage.")]
    public class FrequencyExcursion : BaseAnalytic, IAnalytic
    {
        private Setting m_settings;

        private bool m_isLow;
        private bool m_isHigh;
        private Gemstone.Ticks m_lastNormal;

        private double m_differenceLower;
        private double m_differenceUpper;

        public Type SettingType => typeof(Setting);

        public override int PrevFrames => 1;

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

            [DisplayName("Minimum Duration (s)")]
            [DefaultValue(0.5)]
            public double minDur { get; set; }
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
                new AnalyticOutputDescriptor() { Name = "Excursion Detected", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.EventFlag }, 
            };
        }

        public IEnumerable<string> InputNames()
        {
            
            return new List<string>() { "Signal" };
        }

       
        public override Task<ITimeSeriesValue[]> CompleteComputation(Gemstone.Ticks ticks) 
        {
            return Task.Run(() => CheckLastPoint(ticks));
        }

        private ITimeSeriesValue[] CheckLastPoint(Gemstone.Ticks ticks)
        {
            List<AdaptEvent> result = new List<AdaptEvent>();
            if (!m_isHigh && ! m_isLow)
                return result.ToArray();

            double length = ticks - m_lastNormal;
            if (m_isHigh && length > m_settings.minDur * Gemstone.Ticks.PerSecond)
                result.Add(new AdaptEvent("Excursion Detected", m_lastNormal, length, new KeyValuePair<string, double>("Deviation", m_settings.upper + m_differenceUpper)));
            if (m_isLow && length > m_settings.minDur * Gemstone.Ticks.PerSecond)
                result.Add(new AdaptEvent("Excursion Detected", m_lastNormal, length, new KeyValuePair<string, double>("Deviation", m_settings.lower - m_differenceLower)));

            return result.ToArray();

        }

        public override ITimeSeriesValue[] Compute(IFrame frame, IFrame[] previousFrames, IFrame[] future) 
        {
            List<AdaptEvent> result = new List<AdaptEvent>();

            double value = frame.Measurements["Signal"].Value;

            if (double.IsNaN(value))
                return result.ToArray();

            if (m_settings.excursionType == ExcursionType.Lower || m_settings.excursionType == ExcursionType.UpperAndLower)
            {
               
                // On Low
                if (value < m_settings.lower && !m_isLow)
                {
                    m_isLow = true;
                    m_lastNormal = frame.Timestamp;
                    m_differenceLower = m_settings.lower - value;
                }
                // ON not Low
                else if (value > m_settings.lower && m_isLow)
                {
                    m_isLow = false;
                    double length = frame.Timestamp - m_lastNormal;
                    if (length > m_settings.minDur*Gemstone.Ticks.PerSecond)
                        result.Add(new AdaptEvent("Excursion Detected", m_lastNormal, length,new KeyValuePair<string, double>("Deviation", m_settings.lower - m_differenceLower)));
                }
                else if (value < m_settings.lower && m_isLow)
                {
                    if ((m_settings.lower - value) > m_differenceLower)
                        m_differenceLower = (m_settings.lower - value);
                }


            }
            if (m_settings.excursionType == ExcursionType.Upper || m_settings.excursionType == ExcursionType.UpperAndLower)
            {
                // On High
                if (value > m_settings.upper && !m_isHigh)
                {
                    m_differenceLower = frame.Timestamp;
                    m_differenceUpper = value - m_settings.upper;
                    m_isHigh = true;
                }
                // On not High
                else if (value < m_settings.upper && m_isHigh)
                {
                    m_isHigh = false;
                    double length = frame.Timestamp - m_lastNormal;
                    if (length > m_settings.minDur * Gemstone.Ticks.PerSecond)
                        result.Add(new AdaptEvent("Excursion Detected", m_lastNormal, length, new KeyValuePair<string, double>("Deviation", m_settings.upper + m_differenceUpper)));
                }
                else if (value > m_settings.upper && m_isHigh)
                {
                    if ((value - m_settings.upper) > m_differenceUpper)
                        m_differenceUpper = (value - m_settings.upper);
                }
                
            }

            return result.ToArray();
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
            m_isHigh = false;
            m_isLow = false;
            m_differenceLower = 0;
            m_differenceUpper = 0;
        }

    }
}
