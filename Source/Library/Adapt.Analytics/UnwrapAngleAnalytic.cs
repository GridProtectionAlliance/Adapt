﻿// ******************************************************************************************************
//  UnwrapAngleAnalytic.tsx - Gbtc
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
//  02/02/2022 - A. Hagemeyer
//       Changed to an Unwrap angle analytic
//
// ******************************************************************************************************


using Adapt.Models;
using GemstoneCommon;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Runtime;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace Adapt.DataSources
{

    /// <summary>
    /// unwraps angles
    /// </summary>

    [AnalyticSection(AnalyticSection.DataCleanup)]

    [Description("Unwrap Angle: Unwraps an angle.")]
    public class UnwrapAngle : IAnalytic
    {
        private Setting m_settings;
        private int m_fps;
        private double last;

        public class Setting
        {
            [DisplayName("Angle Unit")]
            public AngleUnit InputUnit { get; set; }
        }

        public Type SettingType => typeof(Setting);

        public int FramesPerSecond => m_fps;

        public int PrevFrames => 0;

        public int FutureFrames => 0;

        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "Angle", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other }
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Original" };
        }

        public Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames)
        {
            return Task.FromResult<ITimeSeriesValue[]>( Compute(frame) );
        }

        public ITimeSeriesValue[] Compute(IFrame frame) 
        {
            double current = GetAngle(frame);
            if (last > 0) 
            {
                while (Math.Abs(current - last) > Math.Abs(current + 360 - last)) 
                {
                    current += 360;
                }
                last = current;
                return new AdaptValue[] { new AdaptValue("Angle", current, frame.Timestamp) };
            }
            if (last < 0)
            {
                while (Math.Abs(current - last) > Math.Abs(current - 360 - last))
                {
                    current -= 360;
                }
                last = current;
                return new AdaptValue[] { new AdaptValue("Angle", current, frame.Timestamp) };
            }
            else 
            {
                if (Math.Abs(current - 360) > Math.Abs(current))
                    current -= 360;
                else
                    current += 360;
                last = current;
                return new AdaptValue[] { new AdaptValue("Angle", current, frame.Timestamp) };
            }
        }

        public double GetAngle(IFrame frame) 
        {
            double original = frame.Measurements["Original"].Value;
            if (m_settings.InputUnit == AngleUnit.Radians)
                return (original * (180 / Math.PI));
            else
                return original;
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