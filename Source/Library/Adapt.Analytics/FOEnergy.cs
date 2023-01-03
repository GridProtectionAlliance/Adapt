// ******************************************************************************************************
//  FOEnergy.tsx - Gbtc
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
//  12/23/2022 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Adapt.Models;
using Gemstone;
using Gemstone.Units;
using GemstoneAnalytic;
using GemstoneCommon;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Adapt.DataSources
{
    /// <summary>
    /// If value = 0 return NaN. Otherwise return the data
    /// </summary>
    
    [AnalyticSection(AnalyticSection.DataFiltering)]
    [Description("DEF Computation: Computes the Dissipating Energy Flow for forced oscillation.")]
    public class FOEnergy : BaseAnalytic, IAnalytic
    {
        private Setting m_settings;
        public class Setting 
        {
            public double Frequency { get; set; }

            [SettingName("Window Size (s)"), DefaultValue(10)]
            public double WindowSize { get; set; } = 10;
        }

        private struct Data
        {
            public double Vm;
            public double P;
            public double Q;
            public double Vp;
        }

        public Type SettingType => typeof(Setting);

        public override int FutureFrames => (int)Math.Ceiling(m_settings?.WindowSize ?? 10.0 * FramesPerSecond);

        private Data[] m_windowData;
        private Ticks m_windowEnd;

        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "DEF", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other } 
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Voltage Magnitude", "Voltage Angle", "Active Power", "Reactive Power" };
        }

        public override ITimeSeriesValue[] Compute(IFrame frame, IFrame[] prev, IFrame[] future) 
        {
            double DEFline = double.NaN;
            if (m_windowEnd == frame.Timestamp || m_windowEnd == Ticks.MinValue)
            {
                m_windowData = new Data[future.Length + 1];
                m_windowEnd = future.Last().Timestamp;
                
                double Vm_mean = future.Select(f => Math.Log(f.Measurements["Voltage Magnitude"].Value)).Average();
                double Vp_mean = future.Select(f => f.Measurements["Voltage Angle"].Value).Average();
                double P_mean = future.Select(f => f.Measurements["Active Power"].Value).Average();
                double Q_mean = future.Select(f => f.Measurements["Reactive Power"].Value).Average();

                m_windowData[0] = new Data
                {
                    Vp = frame.Measurements["Voltage Angle"].Value - Vp_mean,
                    Vm = Math.Log(frame.Measurements["Voltage Magnitude"].Value) - Vm_mean,
                    P = frame.Measurements["Active Power"].Value - P_mean,
                    Q = frame.Measurements["Reactive Power"].Value - Q_mean,
                };

                for (int i = 0; i < future.Length; i++)
                {
                    m_windowData[i+1] = new Data
                    {
                        Vp = future[i].Measurements["Voltage Angle"].Value - Vp_mean,
                        Vm = Math.Log(future[i].Measurements["Voltage Magnitude"].Value) - Vm_mean,
                        P = future[i].Measurements["Active Power"].Value - P_mean,
                        Q = future[i].Measurements["Reactive Power"].Value - Q_mean,
                    };
                }

                WelshPeriodoGramm welshP = new WelshPeriodoGramm(m_windowData.Select(d => d.P).ToArray(), m_windowData.Select(d => d.Vm).ToArray(),
                WindowFunction.rectwin, m_windowData.Count(), 0);

                WelshPeriodoGramm welshQ = new WelshPeriodoGramm(m_windowData.Select(d => d.Q).ToArray(), m_windowData.Select(d => d.Vp).ToArray(),
                    WindowFunction.rectwin, m_windowData.Count(), 0);



                Complex[] Spw = welshP.ComplexMagnitude.Take(welshP.ComplexMagnitude.Count()/2).ToArray();
                Spw = Spw.Select((v,i) => v * (double)FramesPerSecond *(i == 0 ? 1.0D : 2.0D) ).ToArray();

                Complex[] Sqv = welshQ.ComplexMagnitude.Take(welshQ.ComplexMagnitude.Count() / 2).ToArray();
                Sqv = Sqv.Select((v, i) => v * (double)FramesPerSecond * (i == 0 ? 1.0D : 2.0D)).ToArray();

                int fIndex = (int)Math.Round(m_settings.Frequency/(2.0D*(double)FramesPerSecond*Sqv.Count()));

                DEFline = 2.0D * Spw[fIndex].Real + 2.0D * Math.PI * m_settings.Frequency * Sqv[fIndex].Imaginary;
            }
                    
            return new AdaptValue[] { new AdaptValue("DEF", DEFline, frame.Timestamp) };
           
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            m_windowEnd = Ticks.MinValue;
            config.Bind(m_settings);


        }
    }
}
