// ******************************************************************************************************
//  WindRampingAnalysis.tsx - Gbtc
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
//  03/04/2022 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Adapt.Models;
using Gemstone;
using GemstoneAnalytic;
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
    public enum TimeScale
    {
        Short,
        Medium,
        Long
    }

    /// <summary>
    /// Analytic to Detect Frequency Excursion
    /// </summary>

    [AnalyticSection(AnalyticSection.EventDetection)]

    [Description("Wind Ramping Analysis: Detects Ramping of Wind Power")]
    public class WindRampingAnalysis : BaseAnalytic, IAnalytic
    {
        private Setting m_settings;
        private List<DigitalFilter> m_filters;
        private List<FilterState> m_filterStates;

        private double m_trendValue;
        private Ticks m_trendTicks;
        private double m_trendIncrease;
        private double m_prevFiltered;
        public Type SettingType => typeof(Setting);

        public class Setting 
        {
            [DefaultValue(TimeScale.Short)]
            public TimeScale Scale { get; set; }

            [DefaultValue(10)]
            [SettingName("Min Change")]
            public double MinChange { get; set; }

            [DefaultValue(10)]
            [SettingName("Max Change")]
            public double MaxChange { get; set; }

            [DefaultValue(10)]
            [SettingName("Min Duration (s)")]
            public double MinDuration { get; set; }

            [DefaultValue(10)]
            [SettingName("Max Duration (s)")]
            public double MaxDuration { get; set; }
        }

        public WindRampingAnalysis()
        {
        }
        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "Filtered Power", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other }, 
                new AnalyticOutputDescriptor() { Name = "Ramping Occurred", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.EventFlag }
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Power"};
        }


        public override ITimeSeriesValue[] Compute(IFrame frame, IFrame[] previousFrames, IFrame[] future)
        {
            double filtered = frame.Measurements["Power"].Value;

            for (int i =0; i < m_filters.Count; i++)
            {
                if (m_filterStates.Count() < i + 1)
                    m_filterStates.Add(CreateInitialConditions(m_filters[i],filtered));
                FilterState updated;
                filtered = m_filters[i].Filt(filtered, m_filterStates[i], out updated);
                m_filterStates[i] = updated;
            }

            m_prevFiltered = filtered;
            List<ITimeSeriesValue> result = new List<ITimeSeriesValue>() {
                new AdaptValue("Filtered Power", filtered, frame.Timestamp) 
            };

            bool isExtrema = false;

            if (double.IsNaN(m_trendValue))
            {
                m_trendIncrease = double.NaN;
                m_trendValue = filtered;
                m_trendTicks = frame.Timestamp;
            }
            if (double.IsNaN(m_trendIncrease))
            {
                m_trendIncrease = filtered - m_trendValue;
            }
            if (!double.IsNaN(m_trendValue) && !double.IsNaN(m_trendIncrease))
                isExtrema = (m_trendIncrease) * (filtered - m_trendValue) > -1;

            bool isEvent = false;
            if (isExtrema)
            {
                double TrendTime = frame.Timestamp - m_trendTicks;
                double TrendAmount = filtered - m_trendValue;
                
                double DetSlope = (m_settings.MaxChange - m_settings.MinChange) / ((m_settings.MaxDuration - m_settings.MinDuration) * Ticks.PerSecond);
                double DetYint = m_settings.MaxChange - DetSlope * m_settings.MaxDuration*Ticks.PerSecond;
                isEvent = Math.Abs(TrendAmount) > m_settings.MinChange &&
                    (TrendTime < m_settings.MinDuration * Ticks.PerSecond) &&
                    Math.Abs(TrendAmount) > (DetSlope * TrendTime + DetYint);



                if (isEvent)
                    result.Add(new AdaptEvent("Ramping Occurred", m_trendTicks, TrendTime, 
                        new KeyValuePair<string, double>("Power Change", TrendAmount),
                        new KeyValuePair<string, double>("Avg Rate", TrendAmount/TrendTime)
                        ));
           
                m_trendTicks = frame.Timestamp;
                m_trendIncrease = -m_trendIncrease;
                m_trendValue = filtered;
            }
            return result.ToArray();

        }

        private FilterState CreateInitialConditions(DigitalFilter filter, double value)
        {
            m_trendValue = double.NaN;
            int ex = 100000;
            FilterState updated = new FilterState() { StateValue = new double[] { } };

            double[] f = new double[ex];
            for (int i=0; i < ex; i++)
            {
                f[i] = filter.Filt(value, updated, out updated);
            }
            return updated;

        }
        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);

            //Assume it's 30 FPS for now
            if (m_settings.Scale == TimeScale.Short)
            {
                m_filters = new List<DigitalFilter>() {
                    new DigitalFilter(new double[]{ 1,2,1 }, new double[] { 1,-1.99857601179813,0.998619639327290 }, 1.09068822903295e-05),
                    new DigitalFilter(new double[]{ 1,2,1 }, new double[] { 1,-1.99588118005400,0.995924748756856 }, 1.08921757130205e-05),
                    new DigitalFilter(new double[]{ 1,2,1 }, new double[] { 1,-1.99337088446336,0.993414398368195 }, 1.08784762097945e-05),
                    new DigitalFilter(new double[]{ 1,2,1 }, new double[] { 1,-1.99115308480013,0.991196550291937 }, 1.08663729524114e-05),
                    new DigitalFilter(new double[]{ 1,2,1 }, new double[] { 1,-1.98932248822578,0.989365913756935 }, 1.08563827887951e-05),
                    new DigitalFilter(new double[]{ 1,2,1 }, new double[] { 1,-1.98795679220377,0.988000187922731 }, 1.08489297393897e-05),
                    new DigitalFilter(new double[]{ 1,2,1 }, new double[] { 1,-1.98711368281450,0.987157060128967 }, 1.08443286159837e-05),
                    new DigitalFilter(new double[]{ 1,1,0 }, new double[] { 1, -0.9934, 0 }, 0.00329283663694786)
                };
            }

            if (m_settings.Scale == TimeScale.Medium)
            {
                m_filters = new List<DigitalFilter>() {
                    new DigitalFilter(new double[]{ 1,2,1 }, new double[] { 1,-1.99989571659313, 0.999895771445361 }, 1.37130588365364e-08),
                    new DigitalFilter(new double[]{ 1,2,1 }, new double[] { 1,-1.99970793065464, 0.999707985501722 }, 1.37117712095858e-08),
                    new DigitalFilter(new double[]{ 1,2,1 }, new double[] { 1,-1.99957799939819, 0.999578054241707}, 1.37108802856493e-08),
                    new DigitalFilter(new double[]{ 1,1,0 }, new double[] { 1,-0.999765815778955,0 }, 0.000117092110522488),
                };
            }

            if (m_settings.Scale == TimeScale.Long)
            {
                m_filters = new List<DigitalFilter>() {
                    new DigitalFilter(new double[]{ 1,2,1 }, new double[] { 1,-1.99999314315241,0.999993143327873 }, 4.38647653873391e-11),
                    new DigitalFilter(new double[]{ 1,2,1 }, new double[] { 1,- 1.99998126715915,0.999981267334612 }, 4.38645049176171e-11),
                    new DigitalFilter(new double[]{ 1,2,1 }, new double[] { 1, - 1.99997441061547,0.999974410790928}, 4.38643545367615e-11),
                };
            }
            m_filterStates = new List<FilterState>();
        }


        public override Task<ITimeSeriesValue[]> CompleteComputation(Gemstone.Ticks ticks)
        {
            return Task.Run(() => ProcessLastEvent(ticks));
        }

        private ITimeSeriesValue[] ProcessLastEvent(Gemstone.Ticks ticks)
        {

            double TrendTime = ticks - m_trendTicks;
            double TrendAmount = m_prevFiltered - m_trendValue;

            double DetSlope = (m_settings.MaxChange - m_settings.MinChange) / ((m_settings.MaxDuration - m_settings.MinDuration) * Ticks.PerSecond);
            double DetYint = m_settings.MaxChange - DetSlope * m_settings.MaxDuration * Ticks.PerSecond;
            bool isEvent = Math.Abs(TrendAmount) > m_settings.MinChange &&
                (TrendTime < m_settings.MinDuration * Ticks.PerSecond) &&
                Math.Abs(TrendAmount) > (DetSlope * TrendTime + DetYint);



            if (isEvent)
                return new ITimeSeriesValue[] { new AdaptEvent("Ramping Occurred", m_trendTicks, TrendTime,
                    new KeyValuePair<string, double>("Power Change", TrendAmount),
                    new KeyValuePair<string, double>("Avg Rate", TrendAmount / TrendTime)
                    ) };

            return new ITimeSeriesValue[0];
        }
    }
}
