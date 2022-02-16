// ******************************************************************************************************
//  WrapAngleAnalytic.tsx - Gbtc
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
//  01/28/2022 - A. Hagemeyer
//       Changed to a wrap angle analytic
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

    public enum WrapBetweenAngles
    {
        [Description("0 and 360")] upper,
        [Description("-180 and 180")] lower
    }

    /// <summary>
    /// Wraps the angles that are calculated from the data
    /// </summary>

    [AnalyticSection(AnalyticSection.DataCleanup)]

    [Description("Wrap Angle: Wraps an angle.")]
    public class WrapAngle : IAnalytic
    {
        private Setting m_settings;
        private int m_fps;
        private double wrapped;

        public class Setting
        {
            [SettingName("Angle Unit")]
            [DefaultValue(AngleUnit.Degrees)]
            public AngleUnit Unit { get; set; }
            
            [SettingName("Wrap Between")]
            [DefaultValue(WrapBetweenAngles.upper)]
            public WrapBetweenAngles WrapBetween { get; set; }
        }

        public Type SettingType => typeof(Setting);

        public int FramesPerSecond => m_fps;

        public int PrevFrames => 1;

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
        
        public Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] previousFrames)
        {
            return Task.Run(() => Compute(frame));
        }
        
        public ITimeSeriesValue[] Compute(IFrame frame) 
        {
            double angle = frame.Measurements.First().Value.Value;

            if (m_settings.Unit == AngleUnit.Radians)
                angle *= 180 / Math.PI;

            if (m_settings.WrapBetween == WrapBetweenAngles.upper)
            {
                wrapped = angle;
                if (wrapped < 0)
                    while (wrapped < 0)
                        wrapped += 360;

                if (wrapped >= 360)
                    while (wrapped >= 360)
                        wrapped -= 360;

                return new AdaptValue[] { new AdaptValue("Angle", wrapped, frame.Timestamp) };
            }
            if (m_settings.WrapBetween == WrapBetweenAngles.lower)
            {
                wrapped = angle;
                if (wrapped < -180)
                    while (wrapped < -180)
                        wrapped += 360;
                if (wrapped >= 180)
                    while (wrapped >= 180)
                        wrapped -= 360;

                return new AdaptValue[] { new AdaptValue("Angle", wrapped, frame.Timestamp) };
            }

            return new AdaptValue[] { new AdaptValue("Angle", angle, frame.Timestamp) };
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
