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
        private Gemstone.Ticks m_lastCrossing;
        private Gemstone.Ticks m_lastPoint;
        private double m_differenceLower;
        private double m_differenceUpper;
        private ExcursionType m_currentExcursion;

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

       
        public override Task<ITimeSeriesValue[]> CompleteComputation() 
        {
            return Task.Run(() => CheckLastPoint());
        }

        private ITimeSeriesValue[] CheckLastPoint()
        {
            List<AdaptEvent> result = new List<AdaptEvent>();
            if (m_currentExcursion == ExcursionType.UpperAndLower)
                return result.ToArray();

            double length = m_lastPoint - m_lastCrossing;
            if (m_currentExcursion == ExcursionType.Upper)
                result.Add(new AdaptEvent("Excursion Detected", m_lastCrossing, length, new KeyValuePair<string, double>("Deviation", m_settings.upper + m_differenceUpper)));
            if (m_currentExcursion == ExcursionType.Upper)
                result.Add(new AdaptEvent("Excursion Detected", m_lastCrossing, length, new KeyValuePair<string, double>("Deviation", m_settings.lower - m_differenceLower)));

            return result.ToArray();

        }

        public override ITimeSeriesValue[] Compute(IFrame frame, IFrame[] previousFrames, IFrame[] future) 
        {
            m_lastPoint = frame.Timestamp;
            List<AdaptEvent> result = new List<AdaptEvent>();

            double value = frame.Measurements["Signal"].Value;
            double prevValue = previousFrames.FirstOrDefault()?.Measurements["Signal"].Value ?? double.NaN;

            if (double.IsNaN(value))
                return result.ToArray();

            if (m_settings.excursionType == ExcursionType.Lower || m_settings.excursionType == ExcursionType.UpperAndLower)
            {
                // On switch from Ok to in Alarm
                if (value < m_settings.lower && (prevValue >= m_settings.lower || double.IsNaN(prevValue)))
                {
                    m_lastCrossing = frame.Timestamp;
                    m_differenceLower = m_settings.lower - value;
                    m_currentExcursion = ExcursionType.Lower;
                }
                // On switch from in Alarm to OK
                else if (value > m_settings.lower && (prevValue <= m_settings.lower))
                {
                    double length = frame.Timestamp - m_lastCrossing;
                    result.Add(new AdaptEvent("Excursion Detected", m_lastCrossing, length,new KeyValuePair<string, double>("Deviation", m_settings.lower - m_differenceLower)));
                    m_currentExcursion = ExcursionType.UpperAndLower;
                }

                if ((m_settings.lower - value) > m_differenceLower)
                    m_differenceLower = (m_settings.lower - value);
            }
            if (m_settings.excursionType == ExcursionType.Upper || m_settings.excursionType == ExcursionType.UpperAndLower)
            {
                // On switch from Ok to in Alarm
                if (value > m_settings.upper && (prevValue <= m_settings.upper || double.IsNaN(prevValue)))
                {
                    m_lastCrossing = frame.Timestamp;
                    m_differenceUpper = value - m_settings.upper;
                    m_currentExcursion = ExcursionType.Upper;
                }
                // On switch from in Alarm to OK
                else if (value < m_settings.upper && (prevValue >= m_settings.upper))
                {
                    double length = frame.Timestamp - m_lastCrossing;
                    result.Add(new AdaptEvent("Excursion Detected", m_lastCrossing, length, new KeyValuePair<string, double>("Deviation", m_settings.upper + m_differenceUpper)));
                    m_currentExcursion = ExcursionType.UpperAndLower;
                }

                if ((value - m_settings.upper) > m_differenceUpper)
                    m_differenceUpper = (value - m_settings.upper);
            }

            return result.ToArray();
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }

    }
}
