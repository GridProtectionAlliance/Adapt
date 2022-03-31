// ******************************************************************************************************
//  RealComponentAnalytic.tsx - Gbtc
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
//  12/07/2021 - A. Hagemeyer
//       Changed to a real component analytic
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
using System.Numerics;
using System.Runtime;
using System.Threading.Tasks;
using System.Windows;

namespace Adapt.DataSources
{
    public enum AngleUnit 
    {
        Radians,
        Degrees
    }
    /// <summary>
    /// Finds the real component from the complex number given by magnitude and phase
    /// </summary>

    [AnalyticSection(AnalyticSection.DataFiltering)]

    [Description("Real Component: Finds real component from magnitude and phase")]
    public class RealComponent : MultiSignalBaseAnalytic, IAnalytic
    {
        private Setting m_settings;

        public class Setting
        {
            [SettingName("Angle Unit")]
            [DefaultValue(AngleUnit.Degrees)]
            public AngleUnit Unit { get; set; }
        }

        public Type SettingType => typeof(Setting);

        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "Real Component", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other }
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Magnitude", "Phase"};
        }


        public override ITimeSeriesValue[] Compute(IFrame frame, IFrame[] prev, IFrame[] future) 
        {
            ITimeSeriesValue magnitude = frame.Measurements["Magnitude"];
            ITimeSeriesValue phase = frame.Measurements["Phase"];
            if (m_settings.Unit == AngleUnit.Degrees)
                return new AdaptValue[] { new AdaptValue("Real Component", magnitude.Value * Math.Cos(phase.Value), frame.Timestamp) };
            else
                return new AdaptValue[] { new AdaptValue("Real Component", magnitude.Value * Math.Cos((180 / Math.PI) * phase.Value), frame.Timestamp) };
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }
    }
}
