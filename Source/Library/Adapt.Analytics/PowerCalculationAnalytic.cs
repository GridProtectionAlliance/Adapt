// ******************************************************************************************************
//  PowerCalculationAnalytic.tsx - Gbtc
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
//  12/13/2021 - A. Hagemeyer
//       Changed to a power calculation analytic
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
    /// <summary>
    /// Returns the apparent, active and reactive powers
    /// </summary>
    [AnalyticSection(AnalyticSection.DataFiltering)]

    [Description("Power Calculation: Return the apparent, active and reactive powers")]
    public class PowerCalculation : MultiSignalBaseAnalytic, IAnalytic
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
                new AnalyticOutputDescriptor() { Name = "Apparent Power", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other },
                new AnalyticOutputDescriptor() { Name = "Active Power", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other },
                new AnalyticOutputDescriptor() { Name = "Reactive Power", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other }
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Voltage Magnitude", "Voltage Phase", "Current Magnitude", "Current Phase" };
        }

        public override Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames)
        {
            Complex S = GetComplex(frame);
            AdaptValue apparent = new AdaptValue("Apparent Power", S.Magnitude, frame.Timestamp);
            AdaptValue active = new AdaptValue("Active Power", S.Real, frame.Timestamp);
            AdaptValue reactive = new AdaptValue("Reactive Power", S.Imaginary, frame.Timestamp);
            return Task.FromResult<ITimeSeriesValue[]>(new AdaptValue[] { apparent, active, reactive });
        }

     
        public Complex GetComplex(IFrame frame)
        {
            ITimeSeriesValue voltage_mag = frame.Measurements["Voltage Magnitude"];
            ITimeSeriesValue voltage_pha = frame.Measurements["Voltage Phase"];
            ITimeSeriesValue current_mag = frame.Measurements["Current Magnitude"];
            ITimeSeriesValue current_pha = frame.Measurements["Current Phase"];
            if (m_settings.Unit == AngleUnit.Degrees)
            {
                Complex volt = new Complex(voltage_mag.Value * Math.Cos(voltage_pha.Value), voltage_mag.Value * Math.Sin(voltage_pha.Value));
                Complex curr = new Complex(current_mag.Value * Math.Cos(current_pha.Value), current_mag.Value * Math.Sin(current_pha.Value));
                return Complex.Multiply(volt, Complex.Conjugate(curr));
            }
            else 
            {
                Complex volt = new Complex(voltage_mag.Value * Math.Cos((180 / Math.PI) * voltage_pha.Value), voltage_mag.Value * Math.Sin((180 / Math.PI) * voltage_pha.Value));
                Complex curr = new Complex(current_mag.Value * Math.Cos((180 / Math.PI) * current_pha.Value), current_mag.Value * Math.Sin((180 / Math.PI) * current_pha.Value));
                return Complex.Multiply(volt, Complex.Conjugate(curr));
            }
        }
        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }

    }
}
