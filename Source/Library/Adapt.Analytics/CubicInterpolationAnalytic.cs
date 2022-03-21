// ******************************************************************************************************
//  CubicInterpolationAnalytic.tsx - Gbtc
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
//  02/18/2022 - A. Hagemeyer
//       Changed to a cubic interpolation analytic
//
// ******************************************************************************************************


using Adapt.Models;
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
    /// If Value = NaN, return (previous value + next value) / 2, otherwise return the same value
    /// </summary>
    
    [AnalyticSection(AnalyticSection.DataCleanup)]
    [Description("Cubic Interpolation: ")]
    public class CubicInterpolation: IAnalytic
    {
        private Setting m_settings;
        private int m_fps;

        public class Setting 
        {
            [DefaultValue(2)]
            public int Limit { get; set; }
        }

        public Type SettingType => typeof(Setting);

        public int FramesPerSecond => m_fps;

        public int PrevFrames => m_settings.Limit;

        public int FutureFrames => m_settings.Limit;

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

        public Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames)
        {
            return Task.Run(() => Compute(frame, previousFrames, futureFrames));
        }

        public Task CompleteComputation() 
        {
            return Task.Run(() => { });
        }
        public ITimeSeriesValue[] Compute(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames)
        {
            double original = frame.Measurements["Original"].Value;
            List<double> allX = new List<double>();
            List<double> allY = new List<double>();

            if (previousFrames.Length < 2 || futureFrames.Length < 2)
                return new AdaptValue[] { new AdaptValue("Linear", original, frame.Timestamp) };

            allX.Add(previousFrames.FirstOrDefault().Timestamp);
            allY.Add(previousFrames.First().Measurements.First().Value.Value);
            allX.Add(previousFrames[1].Timestamp);
            allY.Add(previousFrames[1].Measurements.First().Value.Value);

            allX.Add(futureFrames.FirstOrDefault().Timestamp);
            allY.Add(futureFrames.First().Measurements.First().Value.Value);
            allX.Add(futureFrames[1].Timestamp);
            allY.Add(futureFrames[1].Measurements.First().Value.Value);

            //if all values exist other than current, use interpolate function
            if (double.IsNaN(original) && allX.All(x => x != double.NaN) && allY.All(y => y != double.NaN))
                return new AdaptValue[] { new AdaptValue("Linear", CubicInterpolate(allX, allY, frame.Timestamp), frame.Timestamp) };
            //if current doesn't exist and previous and current values also don't then find the values that do
            if (double.IsNaN(original) && (allX.Any(x => x == double.NaN || allY.Any(x => x == double.NaN)))) 
            {
                allX.Clear();
                allY.Clear();
                int n_0 = 0;

                while (n_0 < previousFrames.Length) 
                {
                    if (!double.IsNaN(previousFrames[n_0].Measurements.First().Value.Value)) 
                    {
                        allX.Add(previousFrames[n_0].Timestamp);
                        allY.Add(previousFrames[n_0].Measurements.First().Value.Value);
                        break;
                    }
                    n_0++;
                }

                n_0++;
                while (n_0 < previousFrames.Length) 
                {
                    if (!double.IsNaN(previousFrames[n_0].Measurements.First().Value.Value))
                    {
                        allX.Add(previousFrames[n_0].Timestamp);
                        allY.Add(previousFrames[n_0].Measurements.First().Value.Value);
                        break;
                    }
                    n_0++;
                }
                
                int n_1 = 0;
                while (n_1 < futureFrames.Length) 
                {
                    if (!double.IsNaN(futureFrames[n_1].Measurements.First().Value.Value)) 
                    {
                        allX.Add(futureFrames[n_1].Timestamp);
                        allY.Add(futureFrames[n_1].Measurements.First().Value.Value);
                        break;
                    }
                    n_1++;
                }

                n_1++;
                while (n_1 < futureFrames.Length)
                {
                    if (!double.IsNaN(futureFrames[n_1].Measurements.First().Value.Value))
                    {
                        allX.Add(futureFrames[n_1].Timestamp);
                        allY.Add(futureFrames[n_1].Measurements.First().Value.Value);
                        break;
                    }
                    n_1++;
                }

                if (allX.Count < 4 || allY.Count < 4)
                    return new AdaptValue[] { new AdaptValue("Linear", double.NaN, frame.Timestamp) };
                return new AdaptValue[] { new AdaptValue("Linear", CubicInterpolate(allX, allY, frame.Timestamp), frame.Timestamp) };
            }
            return new AdaptValue[] { new AdaptValue("Linear", original, frame.Timestamp) };

        }

        public double CubicInterpolate(List<double> allX, List<double> allY, double x) 
        {
            double answer = 0;
            for (int i = 0; i <= allX.Count - 1; i++)
            {
                double numerator = 1;
                double denominator = 1;
                for (int c = 0; c <= allX.Count - 1; c++)
                {
                    if (c != i)
                    {
                        numerator *= (x - allX[c]);
                        denominator *= (allX[i] - allX[c]);

                    }
                }
                answer += allY[i] * (numerator / denominator);
            }
            return answer;
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }

        public void SetInputFPS(IEnumerable<int> inputFramesPerSecond)
        {
            m_fps = inputFramesPerSecond.FirstOrDefault();
        }
    }
}
