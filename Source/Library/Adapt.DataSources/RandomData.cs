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
using Gemstone.Numeric.EE;
using Gemstone.Units;
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
        public Type SettingType => typeof(RandomDataSettings);
        public event EventHandler<MessageArgs> MessageRecieved;
        public bool SupportProgress => true;
        

        private RandomDataSettings m_settings;
        private AdaptDevice m_pmu;
        private int m_nProcessed;
        private int m_frameCount;
        private Random m_random;
        public void Configure(IConfiguration config)
        {
            m_settings = new RandomDataSettings();
            config.Bind(m_settings);
            m_pmu = new AdaptDevice(m_settings.PMUName);

            m_random = new Random();
        }

        public async IAsyncEnumerable<IFrame> GetData(List<AdaptSignal> signals, DateTime start, DateTime end)
        {
            // For now just generate a triangle wave at fps
            m_frameCount = (int)Math.Floor(m_settings.FramesPerSecond * (end - start).TotalSeconds);
            m_nProcessed = 0;
            long ticks = Ticks.PerSecond / m_settings.FramesPerSecond;
            DateTime current = start;

            for (int i = 0; i < m_frameCount; i++)
            {
                current = current.AddTicks(ticks);
                yield return GenerateFrame(current, signals);
                m_nProcessed++;
            }
        }

        private IFrame GenerateFrame(Ticks Time, List<AdaptSignal> signals)
        {
            List<ITimeSeriesValue> values = new List<ITimeSeriesValue>();

            Phasor Va, Vb, Vc, Vn;
            Phasor Ia, Ib, Ic, In;
            double f;


            f = GetRandom(m_settings.NominalFrequency, m_settings.FreqStandardDev);

            Va = new Phasor(PhasorType.Voltage,Angle.FromDegrees(GetRandom(0, 5)),GetRandom(m_settings.LLBaseVoltage / Math.Sqrt(3), m_settings.VoltageStandardDev));
            Vc = new Phasor(PhasorType.Voltage, Angle.FromDegrees(GetRandom(120.0, 5)), GetRandom(m_settings.LLBaseVoltage / Math.Sqrt(3), m_settings.VoltageStandardDev));
            Vb = new Phasor(PhasorType.Voltage, Angle.FromDegrees(GetRandom(240.0, 5)), GetRandom(m_settings.LLBaseVoltage / Math.Sqrt(3), m_settings.VoltageStandardDev));
            Vn = new Phasor(PhasorType.Voltage, Angle.FromDegrees(GetRandom(0, 5)), GetRandom(0.0D, m_settings.VoltageStandardDev));

            Ia = new Phasor(PhasorType.Current, Angle.FromDegrees(GetRandom(15, 10)), GetRandom(m_settings.CurrentAvg, m_settings.CurrentStandardDev));
            Ib = new Phasor(PhasorType.Current, Angle.FromDegrees(GetRandom(135, 10)), GetRandom(m_settings.CurrentAvg, m_settings.CurrentStandardDev));
            Ic = new Phasor(PhasorType.Current, Angle.FromDegrees(GetRandom(255, 10)), GetRandom(m_settings.CurrentAvg, m_settings.CurrentStandardDev));

            In = Ia + Ib + Ic;

            foreach (AdaptSignal s in signals)
            {
                switch (s.ID)
                {
                    case ("Freq"):
                        values.Add(new AdaptValue(s.ID) { Timestamp = Time, Value = f });
                        break;
                    case ("VA-Phase"):
                        values.Add(new AdaptValue(s.ID) { Timestamp = Time, Value = Va.Value.Phase*180.0D/Math.PI });
                        break;
                    case ("VB-Phase"):
                        values.Add(new AdaptValue(s.ID) { Timestamp = Time, Value = Vb.Value.Phase * 180.0D / Math.PI });
                        break;
                    case ("VC-Phase"):
                        values.Add(new AdaptValue(s.ID) { Timestamp = Time, Value = Vc.Value.Phase * 180.0D / Math.PI });
                        break;
                    case ("VAB-Phase"):
                        values.Add(new AdaptValue(s.ID) { Timestamp = Time, Value = (Vb - Va).Value.Phase * 180.0D / Math.PI });
                        break;
                    case ("VBC-Phase"):
                        values.Add(new AdaptValue(s.ID) { Timestamp = Time, Value = (Vc - Vb).Value.Phase * 180.0D / Math.PI });
                        break;
                    case ("VCA-Phase"):
                        values.Add(new AdaptValue(s.ID) { Timestamp = Time, Value = (Va - Vc).Value.Phase * 180.0D / Math.PI });
                        break;
                    case ("VA-Mag"):
                        values.Add(new AdaptValue(s.ID) { Timestamp = Time, Value = Va.Value.Magnitude });
                        break;
                    case ("VB-Mag"):
                        values.Add(new AdaptValue(s.ID) { Timestamp = Time, Value = Vb.Value.Magnitude });
                        break;
                    case ("VC-Mag"):
                        values.Add(new AdaptValue(s.ID) { Timestamp = Time, Value = Vc.Value.Magnitude });
                        break;
                    case ("VAB-Mag"):
                        values.Add(new AdaptValue(s.ID) { Timestamp = Time, Value = (Vb - Va).Value.Magnitude });
                        break;
                    case ("VBC-Mag"):
                        values.Add(new AdaptValue(s.ID) { Timestamp = Time, Value = (Vc - Vb).Value.Magnitude });
                        break;
                    case ("VCA-Mag"):
                        values.Add(new AdaptValue(s.ID) { Timestamp = Time, Value = (Va - Vc).Value.Magnitude });
                        break;

                }
            }

            return new Frame()
            {
                Published = true,
                Timestamp = Time,
                Measurements = new ConcurrentDictionary<string, ITimeSeriesValue>(values.Select(v => new KeyValuePair<string,ITimeSeriesValue>(v.ID,v)))

            };
        }

        private double GetRandom(double mean, double stdev)
        {
            double zero = m_random.NextDouble();
            double missing = m_random.NextDouble();

            if ((m_settings.Missing / 100.0D) > missing)
                return double.NaN;
            if ((m_settings.ZeroValue / 100.0D) > zero)
                return 0.0D;

            double u1 = 1.0 - m_random.NextDouble();
            double u2 = 1.0 - m_random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + randStdNormal * stdev;
        }

        public IEnumerable<AdaptDevice> GetDevices()
        {
            return new List<AdaptDevice>() { m_pmu };
        }

        public double GetProgress()
        {
            return ((double)m_nProcessed / (double)m_frameCount) * 100.0D;
        }
       
        public IEnumerable<AdaptSignal> GetSignals()
        {
            List<AdaptSignal> signals = new List<AdaptSignal>();

            if (m_settings.IncludeLLVoltages)
            {
                signals.Add(new AdaptSignal("VAB-Phase", "Voltage Phase AB", m_pmu, m_settings.FramesPerSecond) { Phase = GemstoneCommon.Phase.AB, Type = MeasurementType.VoltagePhase});
                signals.Add(new AdaptSignal("VAB-Mag", "Voltage Magnitude AB", m_pmu, m_settings.FramesPerSecond) { Phase = GemstoneCommon.Phase.AB, Type = MeasurementType.VoltageMagnitude });
                signals.Add(new AdaptSignal("VBC-Phase", "Voltage Phase BC", m_pmu, m_settings.FramesPerSecond) { Phase = GemstoneCommon.Phase.BC, Type = MeasurementType.VoltagePhase });
                signals.Add(new AdaptSignal("VBC-Mag", "Voltage Magnitude BC", m_pmu, m_settings.FramesPerSecond) { Phase = GemstoneCommon.Phase.BC, Type = MeasurementType.VoltageMagnitude });

                signals.Add(new AdaptSignal("VCA-Phase", "Voltage Phase CA", m_pmu, m_settings.FramesPerSecond) { Phase = GemstoneCommon.Phase.CA, Type = MeasurementType.VoltagePhase });
                signals.Add(new AdaptSignal("VCA-Mag", "Voltage Magnitude CA", m_pmu, m_settings.FramesPerSecond) { Phase = GemstoneCommon.Phase.CA, Type = MeasurementType.VoltageMagnitude });

            }

            if (m_settings.IncludeLNVoltages)
            {
                signals.Add(new AdaptSignal("VA-Phase", "Voltage Phase AN", m_pmu, m_settings.FramesPerSecond) { Phase = GemstoneCommon.Phase.A, Type = MeasurementType.VoltagePhase });
                signals.Add(new AdaptSignal("VA-Mag", "Voltage Magnitude AN", m_pmu, m_settings.FramesPerSecond) { Phase = GemstoneCommon.Phase.A, Type = MeasurementType.VoltageMagnitude });

                signals.Add(new AdaptSignal("VB-Phase", "Voltage Phase BN", m_pmu, m_settings.FramesPerSecond) { Phase = GemstoneCommon.Phase.B, Type = MeasurementType.VoltagePhase });
                signals.Add(new AdaptSignal("VB-Mag", "Voltage Magnitude BN", m_pmu, m_settings.FramesPerSecond) { Phase = GemstoneCommon.Phase.B, Type = MeasurementType.VoltageMagnitude });

                signals.Add(new AdaptSignal("VC-Phase", "Voltage Phase CN", m_pmu, m_settings.FramesPerSecond) { Phase = GemstoneCommon.Phase.C, Type = MeasurementType.VoltagePhase });
                signals.Add(new AdaptSignal("VC-Mag", "Voltage Magnitude CN", m_pmu, m_settings.FramesPerSecond) { Phase = GemstoneCommon.Phase.C, Type = MeasurementType.VoltageMagnitude });

            }

            signals.Add(new AdaptSignal("Freq", "Frequency", m_pmu, m_settings.FramesPerSecond) { Phase = GemstoneCommon.Phase.NONE, Type = MeasurementType.Frequency, FramesPerSecond = m_settings.FramesPerSecond });

            return signals;
            
        }

       

        public bool Test()
        {
            return true;
        }
    }
}
