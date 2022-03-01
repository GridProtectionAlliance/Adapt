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
    [Description("Linear Interpolation: Calculate unknown points according to previous and future points.")]
    public class LinearInterpolation: IAnalytic
    {
        private Setting m_settings;
        private int m_fps;
        private double last;

        public class Setting
        {
            [DefaultValue(1)]
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

        public ITimeSeriesValue[] Compute(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames) 
        {
            double original = frame.Measurements["Original"].Value;
            double prev = previousFrames.FirstOrDefault()?.Measurements.First().Value.Value ?? double.NaN;
            double future = futureFrames.FirstOrDefault()?.Measurements.First().Value.Value ?? double.NaN;

            double next = 0;
            double value = 0;
            if (!double.IsNaN(original)) 
            {
                return new AdaptValue[] { new AdaptValue("Linear", original, frame.Timestamp) };
            }

            if (double.IsNaN(original) && !double.IsNaN(prev) && !double.IsNaN(future))
            {
                value = GetY(previousFrames.First().Timestamp, prev, futureFrames.First().Timestamp, future, frame.Timestamp);
                return new AdaptValue[] { new AdaptValue("Linear", value, frame.Timestamp) };
            }

            if (double.IsNaN(original) && (double.IsNaN(prev) || double.IsNaN(future)) && previousFrames.Length != 0 && futureFrames.Length != 0) 
            {
                //if current and prev values are both NaN
                if (double.IsNaN(prev) && !double.IsNaN(future)) 
                {
                    //if we have already calculated the last value, use it to calculate the current one
                    if (!double.IsNaN(last)) 
                    {
                        value = GetY(previousFrames.First().Timestamp, last, futureFrames.First().Timestamp, future, frame.Timestamp);
                        last = value;
                        return new AdaptValue[] { new AdaptValue("Linear", value, frame.Timestamp) };
                    }

                    //find next frame that doesn't contain NaN
                    int n = 0;
                    bool contains = false;
                    while (n < previousFrames.Length) 
                    {
                        if (!double.IsNaN(previousFrames[n].Measurements.First().Value.Value)) 
                        {
                            last = previousFrames[n].Measurements.First().Value.Value;
                            contains = true;
                            break;
                        }
                        n++;
                    }

                    //if there are no previousFrames within the set limit that contain a value, return NaN and move to the next one
                    if (!contains)
                        return new AdaptValue[] { new AdaptValue("Linear", double.NaN, frame.Timestamp) };

                    //use the next previousFrame to have a value to calculate the line
                    while (n >= 1) 
                    {
                        previousFrames[n-1].Measurements.First().Value.Value = GetY(previousFrames[n].Timestamp, previousFrames[n].Measurements.First().Value.Value, futureFrames.First().Timestamp, future, previousFrames[n - 1].Timestamp);
                        n--;
                    }
                    value = GetY(previousFrames.First().Timestamp, previousFrames.First().Measurements.First().Value.Value, futureFrames.First().Timestamp, future, frame.Timestamp);
                    last = value;
                    return new AdaptValue[] { new AdaptValue("Linear", value, frame.Timestamp) };
                }
                //if current and future value are both NaN
                if (double.IsNaN(future) && !double.IsNaN(prev)) 
                {
                    //find the next frame that has an actual value
                    int n = 0;
                    bool contains = false;
                    while (n < futureFrames.Length) 
                    {
                        if (!double.IsNaN(futureFrames[n].Measurements.First().Value.Value)) 
                        {
                            contains = true;
                            break;
                        }
                        n++;
                    }

                    //if there are no futureFrames that contain a value, return NaN and move on
                    if (!contains)
                        return new AdaptValue[] { new AdaptValue("Linear", double.NaN, frame.Timestamp) };

                    //use the next futureFrame with a value to find the line and then return it
                    while (n >= 1)
                    {
                        futureFrames[n-1].Measurements.First().Value.Value = GetY(previousFrames.First().Timestamp, prev, futureFrames[n].Timestamp, futureFrames[n].Measurements.First().Value.Value, futureFrames[n - 1].Timestamp);
                        n--;
                    }
                    value = GetY(previousFrames.First().Timestamp, prev, futureFrames.First().Timestamp, futureFrames.First().Measurements.First().Value.Value, frame.Timestamp);
                    last = value;
                    return new AdaptValue[] { new AdaptValue("Linear", value, frame.Timestamp) };
                }
                //if current, future and previous value are all NaN
                if (double.IsNaN(prev) && double.IsNaN(future))
                {
                    //we have the last value but not current or future so do the same as the previous case
                    if (!double.IsNaN(last))
                    {
                        //find the next frame that has an actual value
                        int x = 0;
                        bool contains = false;
                        while (x < futureFrames.Length)
                        {
                            if (!double.IsNaN(futureFrames[x].Measurements.First().Value.Value))
                             {
                                next = futureFrames[x].Measurements.First().Value.Value;
                                contains = true;
                                break;
                             }
                             x++;
                         }

                         //if there are no futureFrames that contain a value, return NaN and move on
                         if (!contains)
                            return new AdaptValue[] { new AdaptValue("Linear", double.NaN, frame.Timestamp) };

                        //use the next futureFrame with a value to find the line and then return it
                        while(x >= 1)
                        {
                            futureFrames[x-1].Measurements.First().Value.Value = GetY(previousFrames.First().Timestamp, prev, futureFrames[x].Timestamp, futureFrames[x].Measurements.First().Value.Value, futureFrames[x-1].Timestamp);
                            x--;
                        }
                        value = GetY(previousFrames.First().Timestamp, prev, futureFrames.First().Timestamp, futureFrames.First().Measurements.First().Value.Value, frame.Timestamp);
                        last = value;
                        return new AdaptValue[] { new AdaptValue("Linear", value, frame.Timestamp) };
                    }

                    //find the next futureFrame that has a real value
                    int n_next = 0;
                    bool future_contains = false;
                    while (n_next < futureFrames.Length) 
                    {
                        if (!double.IsNaN(futureFrames[n_next].Measurements.First().Value.Value)) 
                        {
                            next = futureFrames[n_next].Measurements.First().Value.Value;
                            future_contains = true;
                            break;
                        }
                        n_next++;
                    }

                    //if there are no futureFrames that contain real values, then return NaN
                    if (!future_contains)
                        return new AdaptValue[] { new AdaptValue("Linear", double.NaN, frame.Timestamp) };

                    int n_prev = 0;
                    bool prev_contains = false;
                    while (n_prev < previousFrames.Length) 
                    {
                        if (!double.IsNaN(previousFrames[n_prev].Measurements.First().Value.Value)) 
                        {
                            last = previousFrames[n_prev].Measurements.First().Value.Value;
                            prev_contains = true;
                            break;
                        }
                        n_prev++;
                    }

                    //if there are no previousFrames that contain values, then return NaN
                    if (!prev_contains)
                        return new AdaptValue[] { new AdaptValue("Linear", double.NaN, frame.Timestamp) };



                    value = GetY(previousFrames[n_prev].Timestamp, last, futureFrames[n_next].Timestamp, next, frame.Timestamp);
                    
                    return new AdaptValue[] { new AdaptValue("Linear", value, frame.Timestamp) };
                }
            }

            return new AdaptValue[] { new AdaptValue("Linear", original, frame.Timestamp) };
        }

        public double GetY(double x1, double y1, double x2, double y2, double x) 
        {
            double slope = (y2 - y1) / (x2 - x1);
            double b = y1 - (slope * x1);
            return (slope * x) + b;
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
