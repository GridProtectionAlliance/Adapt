// ******************************************************************************************************
//  PdatImport.tsx - Gbtc
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
//  03/29/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Adapt.Models;
using GemstoneCommon;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Adapt.DataSources
{
    /// <summary>
    /// Represents a data source adapter that imports data from pdat Files.
    /// </summary>
    [Description("PDAT Import: Imports data from a set of pdat files.")]
    public class PdatImporter : IDataSource
    {
        public void Configure(IConfiguration config)
        {
            return;
        }

        public IEnumerable<IFrame> GetData(List<AdaptSignal> signals, DateTime start, DateTime end)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<AdaptDevice> GetDevices()
        {
            return new List<AdaptDevice>();
        }

        public Type GetSettingType()
        {
            return null;
        }

        public IEnumerable<AdaptSignal> GetSignals()
        {
            return new List<AdaptSignal>();
        }

        public bool Test()
        {
            return false;
        }
    }
}
