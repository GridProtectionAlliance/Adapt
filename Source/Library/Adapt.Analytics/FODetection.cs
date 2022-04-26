// ******************************************************************************************************
//  FODetection.tsx - Gbtc
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
//  04/11/2022 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Adapt.Models;
using GemstoneAnalytic;
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

    [Description("Forced Oscillation Detection: Detects FOs based on the Welsh Periodogram.")]
    public class FODetection : BaseAnalytic, IAnalytic
    {
        private Setting m_settings;

        private int m_activeCount = 0;
        public Type SettingType => typeof(Setting);

        public override int PrevFrames => (int)Math.Floor((double)(m_settings?.windowSize ?? 1)*m_fps);

        public class Setting 
        {
           

            [DisplayName("Minimum Frequency (Hz)")]
            [DefaultValue(0)]
            public double minFreq { get; set; }

            [DisplayName("Maximum Frequency (Hz)")]
            [DefaultValue(30)]
            public double maxfreq { get; set; }

            [DisplayName("Window Size (s)")]
            [DefaultValue(600)]
            public double windowSize { get; set; }

            [DisplayName("Welch Window Size (s)")]
            [DefaultValue(200)]
            public double FFTWindowSize { get; set; }

            [DisplayName("Window Function")]
            [DefaultValue(WindowFunction.hann)]
            public WindowFunction PeriodogramWindow { get; set; }

            [DisplayName("Probability of false alarm")]
            [DefaultValue(0.01)]
            public double Pfa { get; set; }

            [DisplayName("Freq Tolerance (Hz)")]
            [DefaultValue(0.2)]
            public double FrequencyTolerance { get; set; }

        }

     
        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "FO Detected", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.EventFlag }, 
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
            
            return result.ToArray();

        }

        public override ITimeSeriesValue[] Compute(IFrame frame, IFrame[] previousFrames, IFrame[] future) 
        {
            if (m_activeCount > 0)
                m_activeCount--;
            if (m_activeCount > 0)
                return new ITimeSeriesValue[0];

            List<AdaptEvent> result = new List<AdaptEvent>();

            if (previousFrames.Count() < PrevFrames)
                return result.ToArray();

            double[] signal = previousFrames.Select(item => (double.IsNaN(item.Measurements["Signal"].Value)? 60.0D : item.Measurements["Signal"].Value)).ToArray();

            
            // set active counter to avoid re-running every window
            m_activeCount = signal.Length;

            WelshPeriodoGramm signalWPG = new WelshPeriodoGramm(signal,m_settings.PeriodogramWindow, (int)m_settings.windowSize*m_fps, 0);

            WelshPeriodoGramm ambientWPG = new WelshPeriodoGramm(signal, m_settings.PeriodogramWindow, (int)m_settings.FFTWindowSize * m_fps, (int)m_settings.FFTWindowSize * m_fps/2, 3);

            // Reduce to only Frequencies of interest
            int NFreq = signalWPG.Frequency.Where(item => item*m_fps/(2*Math.PI) < m_settings.maxfreq && item * m_fps / (2 * Math.PI) < m_settings.minFreq).Count();
            
            // Compute Threshold

            double[] threshold = ambientWPG.Power.Select(v => v*Math.Log(m_settings.Pfa / NFreq)).ToArray();

            // If Exceeds Threshold compute Freq, Ammplitude
            double[] relevantFreq = signalWPG.Frequency.Select(item => item * m_fps / (2 * Math.PI))
                .Where((f,i) => f < m_settings.maxfreq && f < m_settings.minFreq && threshold[i] < signalWPG.Power[i])
                .ToArray();

            double[] relevantSignal = signalWPG.Power
                .Where((v, i) => signalWPG.Frequency[i]*m_fps/ (2* Math.PI) < m_settings.maxfreq &&
                    signalWPG.Frequency[i] * m_fps / (2 * Math.PI) < m_settings.minFreq && threshold[i] < v)
                .ToArray();

            double[] relevantAmbient = ambientWPG.Power
                .Where((v, i) => signalWPG.Frequency[i] * m_fps / (2 * Math.PI) < m_settings.maxfreq &&
                    signalWPG.Frequency[i] * m_fps / (2 * Math.PI) < m_settings.minFreq && threshold[i] < signalWPG.Power[i])
                .ToArray();
            

            bool foundEvent = relevantFreq.Any();

            List<int> maxIndices = new List<int>();

            if (foundEvent)
            {
                double lastFreq = -9999.0D;
                
                for (int i=0; i < relevantSignal.Count(); i ++)
                {
                    if (relevantFreq[i] - lastFreq < m_settings.FrequencyTolerance)
                        if (relevantSignal[maxIndices.Last()] < relevantSignal[i])
                            maxIndices[maxIndices.Count() - 1] = i;
                    maxIndices.Add(i);
                }

                double rmsNoise = Math.Sqrt(maxIndices.Average(i => relevantAmbient[i]));

                foreach (int idx in maxIndices)
                {
                    double N = (double)m_settings.windowSize * m_fps;
                    double U = 1.0D / N * WindowingFunctions.GetPower(m_settings.PeriodogramWindow, (int)m_settings.windowSize * m_fps);
                    double CG = 1 / N * WindowingFunctions.Create(m_settings.PeriodogramWindow, (int)m_settings.windowSize * m_fps).Sum();

                    double amplitued_est = Math.Sqrt((relevantSignal[idx] - relevantAmbient[idx])*4.0D * U / N / (CG*CG));
                    double frequency = relevantFreq[idx];
                    
                    double rmsSignal = amplitued_est / Math.Sqrt(2);
                    double snrEst = 20 * Math.Log10(rmsSignal/rmsNoise);

                    // Add Event
                    result.Add(new AdaptEvent("FO Detected", previousFrames.First().Timestamp, frame.Timestamp - previousFrames.First().Timestamp,
                        new KeyValuePair<string, double>("Amplitude Estimate", amplitued_est),
                        new KeyValuePair<string, double>("Frequency Estimate", frequency),
                        new KeyValuePair<string, double>("Signal to Noise Ratio", snrEst),
                        new KeyValuePair<string, double>("Ambient Noise Spectrum", relevantAmbient[idx]),
                        new KeyValuePair<string, double>("Signal PSD", relevantSignal[idx])
                        ));
                }
                
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
