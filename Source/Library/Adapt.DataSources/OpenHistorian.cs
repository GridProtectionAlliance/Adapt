// ******************************************************************************************************
//  OpenHistorian.tsx - Gbtc
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
//  06/02/2021 - C. Lackner
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
    /// Represents a data source adapter that imports data from an openHistorian Instance.
    /// Note that this requires OH 2.8.5+
    /// </summary>
    [Description("OpenHistorian: Imports Phasor data from an OpenHistorian.")]
    public class OpenHistorian : IDataSource
    {
        
        #region [ Members ]
        private OpenHistorianSettings m_settings;
        
        #endregion

        #region [ Constructor ]

        public OpenHistorian()
        {}
        #endregion

        #region [ Methods ]
        public void Configure(IConfiguration config)
        {
            m_settings = new OpenHistorianSettings();
            config.Bind(m_settings);

          
        }

        public async IAsyncEnumerable<IFrame> GetData(List<AdaptSignal> signals, DateTime start, DateTime end)
        {
            yield break;
        }

      

        public IEnumerable<AdaptDevice> GetDevices()
        {
            
                return new List<AdaptDevice>();

        }

        public double GetProgress()
        {
            throw new NotImplementedException();
        }

        public Type GetSettingType()
        {
            return typeof(OpenHistorianSettings);
        }

        public IEnumerable<AdaptSignal> GetSignals()
        {
                return new List<AdaptSignal>();

        }

        public bool SupportProgress()
        {
            return false;
        }

        public bool Test()
        {
            if (!Directory.Exists(m_settings.RootFolder))
                return false;

            return true;
        }

        
        #endregion

        #region [ static ]
        
        #endregion
    }
}
