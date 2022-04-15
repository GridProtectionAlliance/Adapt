﻿// ******************************************************************************************************
//  CustomFilter.tsx - Gbtc
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
//  04/15/2022 - C. Lackner
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
    /// If value = 0 return NaN. Otherwise return the data
    /// </summary>
    
    [AnalyticSection(AnalyticSection.SignalProcessing)]
    [Description("Custom: Applies custom digital LTI Filter.")]
    public class CustomFilter : BaseAnalytic, IAnalytic
    {
        private Setting m_settings;
        public class Setting 
        { 

            [DisplayName("Filter")]
            [DefaultValue(null)]
            [CustomConfigurationEditor("GemstoneWPF.dll", "GemstoneWPF.Editors.WindowStartup", "type=GemstoneWPF.Editors.DigitalFilterWindow; assembly=GemstoneWPF.dll")]
            public DigitalFilter Filter { get; set; }
        }

        private DigitalFilter m_filter;
        private FilterState m_state;
        public Type SettingType => typeof(Setting);


        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "Filtered", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other } 
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Original" };
        }

        public override ITimeSeriesValue[] Compute(IFrame frame, IFrame[] prev, IFrame[] future) 
        {
            double value = frame.Measurements["Original"].Value;
            if (double.IsNaN(value))
                return new ITimeSeriesValue[0];

            if (m_state == null)
                m_state = CreateInitialConditions(m_filter, value);

            FilterState updated;
            double filtered = m_filter.Filt(value, m_state, out updated);
            m_state = updated;

            return new AdaptValue[] { new AdaptValue("Filtered", filtered, frame.Timestamp) };
            
        }

        private FilterState CreateInitialConditions(DigitalFilter filter, double value)
        {
            int ex = 100000;
            FilterState updated = new FilterState() { StateValue = new double[] { } };

            double[] f = new double[ex];
            for (int i = 0; i < ex; i++)
            {
                f[i] = filter.Filt(value, updated, out updated);
            }
            return updated;

        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);

            m_filter = m_settings.Filter?? new DigitalFilter(new double[] { 1.0D }, new double[] { 1.0D });
        }
    }
}
