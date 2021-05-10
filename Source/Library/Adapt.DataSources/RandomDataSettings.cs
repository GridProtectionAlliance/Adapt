// ******************************************************************************************************
//  RadomDataSettings.tsx - Gbtc
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
//  03/25/2020 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Adapt.Models;
using GemstoneCommon;
using System;
using System.ComponentModel;

namespace Adapt.DataSources
{
    /// <summary>
    /// The Settings for a <see cref="RandomData"/> Settings Source
    /// </summary>
    public class RandomDataSettings
    {
       

        [SettingName("PMU")]
        public string PMUName { get; set; }

        [DefaultValue(true)]
        public bool IncludeLNVoltages { get; set; }

        [DefaultValue(false)]
        public bool IncludeLLVoltages { get; set; }

        [DefaultValue(false)]
        public bool IncludeSeqVoltages { get; set; }

        [DefaultValue(15)]
        public int FramesPerSecond { get; set; }

        [DefaultValue(345)]
        public double LLBaseVoltage { get; set; }

        [DefaultValue(20)]
        public double CurrentAvg { get; set; }

        [DefaultValue(12)]
        public double VoltageStandardDev { get; set; }

        [DefaultValue(0.05)]
        public double CurrentStandardDev { get; set; }

        [DefaultValue(0.05)]
        public double FreqStandardDev { get; set; }

        [DefaultValue(60.0)]
        public double NominalFrequency { get; set; }

    }
}
