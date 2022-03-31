// ******************************************************************************************************
//  LinearInterpolationAnalytic.tsx - Gbtc
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
//  02/16/2022 - A. Hagemeyer
//       Changed to a Linear interpolation analytic
//
// ******************************************************************************************************


using Adapt.Models;
using GemstoneCommon;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;

namespace Adapt.DataSources
{
    /// <summary>
    /// If Value = NaN, return (previous value + next value) / 2, otherwise return the same value
    /// </summary>
    
    [AnalyticSection(AnalyticSection.DataCleanup)]
    [Description("Linear Interpolation: interpolate missing Data points.")]
    public class LinearInterpolation: BaseAnalytic, IAnalytic
    {
        private Setting m_settings;

        public class Setting
        {
            [DefaultValue(1)]
            [SettingName("Maximum Number of Consecutive Missing Points")]
            public int Limit { get; set; }
        }

        public Type SettingType => typeof(Setting);

        public override int PrevFrames => m_settings?.Limit ?? 1;

        public override int FutureFrames => m_settings?.Limit ?? 1;

        public IEnumerable<AnalyticOutputDescriptor> Outputs()
        {
            return new List<AnalyticOutputDescriptor>() { 
                new AnalyticOutputDescriptor() { Name = "Linear", FramesPerSecond = 0, Phase = Phase.NONE, Type = MeasurementType.Other } 
            };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Original" };
        }

        public override ITimeSeriesValue[] Compute(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames) 
        {
            double original = frame.Measurements.First().Value.Value; ;
            double prev = previousFrames.FirstOrDefault()?.Measurements.First().Value.Value ?? double.NaN;
            double future = futureFrames.FirstOrDefault()?.Measurements.First().Value.Value ?? double.NaN;

            if (double.IsNaN(original) && !double.IsNaN(prev) && !double.IsNaN(future))
                return new AdaptValue[] { new AdaptValue("Linear", GetY(previousFrames.First().Timestamp, prev, futureFrames.First().Timestamp, future, frame.Timestamp), frame.Timestamp) };
            if (double.IsNaN(original) && (double.IsNaN(prev) || double.IsNaN(future))) 
            {
                int n_prev = 0;
                int n_future = 0;

                while (n_prev < previousFrames.Length) 
                {
                    if (!double.IsNaN(previousFrames[n_prev].Measurements.First().Value.Value)) 
                    {
                        prev = previousFrames[n_prev].Measurements.First().Value.Value;
                        break;
                    }
                    n_prev++;
                }

                while (n_future < futureFrames.Length) 
                {
                    if (!double.IsNaN(futureFrames[n_future].Measurements.First().Value.Value)) 
                    {
                        future = futureFrames[n_future].Measurements.First().Value.Value;
                        break;
                    }
                    n_future++;
                }

                if (double.IsNaN(prev) || double.IsNaN(future))
                    return new AdaptValue[] { new AdaptValue("Linear", double.NaN, frame.Timestamp) };
                return new AdaptValue[] { new AdaptValue("Linear", GetY(previousFrames[n_prev].Timestamp, prev, futureFrames[n_future].Timestamp, future, frame.Timestamp), frame.Timestamp) };
            }
            return new AdaptValue[] { new AdaptValue("Linear", original, frame.Timestamp) };
        }

        public double GetY(double x1, double y1, double x2, double y2, double x) 
        {
            return y1 + ((x - x1) * ((y2 - y1) / (x2 - x1)));
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }

    }
}
