// ******************************************************************************************************
//  RadomData.tsx - Gbtc
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
//  03/25/2020 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Adapt.Models;
using Gemstone;
using GemstoneCommon;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Adapt.DataSources
{
    /// <summary>
    /// Represents an data source adapter that creates random measurements.
    /// </summary>
    [Description("Random Data: Creates a Random Dataset based on nominal value and Gaussian Noise")]
    public class RandomData : IDataSource
    {

        private RandomDataSettings m_settings;
        private AdaptDevice m_pmu;
        private int m_nProcessed;
        private int m_frameCount;
        public void Configure(IConfiguration config)
        {
            m_settings = new RandomDataSettings();
            config.Bind(m_settings);
            m_pmu = new AdaptDevice(m_settings.PMUName);
        }

        public IEnumerable<IFrame> GetData(List<AdaptSignal> signals, DateTime start, DateTime end)
        {
            // For now just generate a triangle wave at fps
            m_frameCount = (int)Math.Floor(m_settings.FramesPerSecond * (end - start).TotalSeconds);
            m_nProcessed = 0;
            long ticks = Ticks.PerSecond / m_settings.FramesPerSecond;
            double val = 0;
            DateTime current = start;
            for (int i = 0; i < m_frameCount; i++)
            {
                current = current.AddTicks(ticks);
                val = (val+1.0D)%100.0D;
                yield return new Frame()
                {
                    Published = true,
                    Timestamp = current,
                    Measurements = new ConcurrentDictionary<string,ITimeSeriesValue>(signals.Select(item => new KeyValuePair<string,ITimeSeriesValue>(item.ID, new AdaptValue(item.ID) { Timestamp = current, Value = val })))

                };
                m_nProcessed++;
            }
        }

        public IEnumerable<AdaptDevice> GetDevices()
        {
            return new List<AdaptDevice>() { m_pmu };
        }

        public double GetProgress()
        {
            return ((double)m_nProcessed / (double)m_frameCount) * 100.0D;
        }

        public Type GetSettingType()
        {
            return typeof(RandomDataSettings);
        }

        public IEnumerable<AdaptSignal> GetSignals()
        {
            List<AdaptSignal> signals = new List<AdaptSignal>();

            if (m_settings.IncludeLLVoltages)
            {
                signals.Add(new AdaptSignal("VAB-Phase", "Voltage Phase AB", m_pmu) { Phase = GemstoneCommon.Phase.AB, Type = MeasurementType.VoltagePhase, FramesPerSecond=m_settings.FramesPerSecond});
                signals.Add(new AdaptSignal("VAB-Mag", "Voltage Magnitude AB", m_pmu) { Phase = GemstoneCommon.Phase.AB, Type = MeasurementType.VoltageMagnitude, FramesPerSecond = m_settings.FramesPerSecond });
                signals.Add(new AdaptSignal("VBC-Phase", "Voltage Phase BC", m_pmu) { Phase = GemstoneCommon.Phase.BC, Type = MeasurementType.VoltagePhase, FramesPerSecond = m_settings.FramesPerSecond });
                signals.Add(new AdaptSignal("VBC-Mag", "Voltage Magnitude BC", m_pmu) { Phase = GemstoneCommon.Phase.BC, Type = MeasurementType.VoltageMagnitude, FramesPerSecond = m_settings.FramesPerSecond });

                signals.Add(new AdaptSignal("VCA-Phase", "Voltage Phase CA", m_pmu) { Phase = GemstoneCommon.Phase.CA, Type = MeasurementType.VoltagePhase, FramesPerSecond = m_settings.FramesPerSecond });
                signals.Add(new AdaptSignal("VCA-Mag", "Voltage Magnitude CA", m_pmu) { Phase = GemstoneCommon.Phase.CA, Type = MeasurementType.VoltageMagnitude, FramesPerSecond = m_settings.FramesPerSecond });

            }

            if (m_settings.IncludeLNVoltages)
            {
                signals.Add(new AdaptSignal("VA-Phase", "Voltage Phase AN", m_pmu) { Phase = GemstoneCommon.Phase.A, Type = MeasurementType.VoltagePhase, FramesPerSecond = m_settings.FramesPerSecond });
                signals.Add(new AdaptSignal("VA-Mag", "Voltage Magnitude AN", m_pmu) { Phase = GemstoneCommon.Phase.A, Type = MeasurementType.VoltageMagnitude, FramesPerSecond = m_settings.FramesPerSecond });

                signals.Add(new AdaptSignal("VB-Phase", "Voltage Phase BN", m_pmu) { Phase = GemstoneCommon.Phase.B, Type = MeasurementType.VoltagePhase, FramesPerSecond = m_settings.FramesPerSecond });
                signals.Add(new AdaptSignal("VB-Mag", "Voltage Magnitude BN", m_pmu) { Phase = GemstoneCommon.Phase.B, Type = MeasurementType.VoltageMagnitude, FramesPerSecond = m_settings.FramesPerSecond });

                signals.Add(new AdaptSignal("VC-Phase", "Voltage Phase CN", m_pmu) { Phase = GemstoneCommon.Phase.C, Type = MeasurementType.VoltagePhase, FramesPerSecond = m_settings.FramesPerSecond });
                signals.Add(new AdaptSignal("VC-Mag", "Voltage Magnitude CN", m_pmu) { Phase = GemstoneCommon.Phase.C, Type = MeasurementType.VoltageMagnitude, FramesPerSecond = m_settings.FramesPerSecond });

            }

            signals.Add(new AdaptSignal("Freq", "Frequency", m_pmu) { Phase = GemstoneCommon.Phase.NONE, Type = MeasurementType.Frequency, FramesPerSecond = m_settings.FramesPerSecond });

            return signals;
            
        }

        public bool SupportProgress()
        {
            return true;
        }

        public bool Test()
        {
            return true;
        }
    }
}
