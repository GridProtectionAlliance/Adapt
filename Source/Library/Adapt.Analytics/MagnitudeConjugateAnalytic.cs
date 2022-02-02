﻿// ******************************************************************************************************
//  MagnitudeConjugateAnalytic.tsx - Gbtc
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
//  12/08/2021 - A. Hagemeyer
//       Changed to complex conjugate analytic
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
    /// Returns the magnitude and phase of the complex conjugate of the complex number
    /// </summary>

    [AnalyticSection(AnalyticSection.SignalProcessing)]

    [Description("Complex Conjugate (Magnitude/Phase): Return the magnitude & phase of the conjugate of the complex number")]
    public class MagnitudeConjugate : IAnalytic
    {
        private Setting m_settings;
        private int m_fps;
        public class Setting
        {
            public AngleUnit Unit { get; set; }
        }

        public Type SettingType => typeof(Setting);

        public int FramesPerSecond => m_fps;

        public int PrevFrames => 0;

        public int FutureFrames => 0;

        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "Complex Conjugate (Magnitude)", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other },
                new AnalyticOutputDescriptor() { Name = "Complex Conjugate (Phase)", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other }
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Magnitude", "Phase"};
        }

        public Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames)
        {
            AdaptValue magnitude = new AdaptValue("Complex Conjugate (Magnitude)", Complex.Conjugate(GetComplex(frame)).Magnitude, frame.Timestamp);
            AdaptValue phase = new AdaptValue("Complex Conjugate (Phase)", Complex.Conjugate(GetComplex(frame)).Phase, frame.Timestamp);
            return Task.FromResult<ITimeSeriesValue[]>(new AdaptValue[] { magnitude, phase });
        }

        public Complex GetComplex(IFrame frame) 
        {
            ITimeSeriesValue magnitude = frame.Measurements["Magnitude"];
            ITimeSeriesValue phase = frame.Measurements["Phase"];
            if (m_settings.Unit == AngleUnit.Degrees)
                return new Complex(magnitude.Value * Math.Cos(phase.Value), magnitude.Value * Math.Sin(phase.Value));
            else 
                return new Complex(magnitude.Value * Math.Cos((180 / Math.PI) * phase.Value), magnitude.Value * Math.Sin((180 / Math.PI) * phase.Value));
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }

        public int GetGCD(int a, int b)
        {
            int remainder;

            while (b != 0)
            {
                remainder = a % b;
                a = b;
                b = remainder;
            }

            return a;
        }

        public void SetInputFPS(IEnumerable<int> inputFramesPerSecond)
        {
            m_fps = inputFramesPerSecond.Aggregate((S, val) => S * val / GetGCD(S, val));
        }
    }
}
