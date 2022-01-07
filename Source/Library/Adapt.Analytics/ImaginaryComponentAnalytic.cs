// ******************************************************************************************************
//  PassThrough.tsx - Gbtc
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
//       Changed to an imaginary component analytic
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
    /// Finds the imaginary component of the complex number given magnitude and phase
    /// </summary>

    [AnalyticSection(AnalyticSection.DataCleanup)]

    [Description("Imaginary Component: Find imaginary component from magnitude and phase")]
    public class ImaginaryComponent : IAnalytic
    {
        private Setting m_settings;
        private int m_fps;
        public class Setting
        {
            public AngleUnit Unit { get; set;  }
        }

        public Type SettingType => typeof(Setting);

        public int FramesPerSecond => m_fps;

        int IAnalytic.PrevFrames => 0;

        int IAnalytic.FutureFrames => 0;

        public IEnumerable<string> OutputNames()
        {
            return new List<string>() { "Imaginary Component" };
        }

        public IEnumerable<string> InputNames()
        {
            return new List<string>() { "Magnitude", "Phase"};
        }

        public Task<ITimeSeriesValue[]> Run(IFrame frame, IFrame[] previousFrames, IFrame[] futureFrames)
        {
            AdaptValue result = new AdaptValue("Imaginary Component", getImaginaryComponent(frame), frame.Timestamp);
            return Task.FromResult<ITimeSeriesValue[]>(new AdaptValue[] { result });
        }

        public double getImaginaryComponent(IFrame frame) 
        {
            ITimeSeriesValue magnitude = frame.Measurements["Magnitude"];
            ITimeSeriesValue phase = frame.Measurements["Phase"];
            if (m_settings.Unit == AngleUnit.Degrees)
                return magnitude.Value * Math.Sin(phase.Value);
            else
                return magnitude.Value * Math.Sin((180 / Math.PI) * phase.Value);
        }

        public void Configure(IConfiguration config)
        {
            m_settings = new Setting();
            config.Bind(m_settings);
        }

        public void SetInputFPS(IEnumerable<int> inputFramesPerSecond)
        {
            m_fps = inputFramesPerSecond.FirstOrDefault();
            foreach (int i in inputFramesPerSecond)
            {
                if (i > m_fps)
                    m_fps = i;
            }
        }
    }
}
