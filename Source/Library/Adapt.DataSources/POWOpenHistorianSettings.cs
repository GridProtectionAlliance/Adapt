// ******************************************************************************************************
//  POWOpenHistorianSettings.tsx - Gbtc
//
//  Copyright © 2022, Grid Protection Alliance.  All Rights Reserved.
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
//  05/31/2022 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Adapt.Models;
using Gemstone;
using GemstoneCommon;
using GemstonePhasorProtocolls;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Adapt.DataSources
{
   

    /// <summary>
    /// The Settings for a <see cref="POWOpenHistorian"/> Data Source
    /// </summary>
    public class POWOpenHistorianSettings
    {

        [DefaultValue("localhost:8180\\")]
        public string Server { get; set; }
        public string User { get; set; }

        [PasswordPropertyText]
        public string Password { get; set; }

        [DefaultValue("PPA")]
        public string Instance { get; set; }
        
        [DefaultValue(NamingConvention.PointTag)]
        public NamingConvention NameField { get; set; }

        [DefaultValue(30)]
        [SettingName("Sampling rate (fps)")]
        public double SamplingFrequency { get; set; }

        [DefaultValue(0.5)]
        [SettingName("Window Size (s)")]
        public double WindowSize { get; set; }


    }
}
