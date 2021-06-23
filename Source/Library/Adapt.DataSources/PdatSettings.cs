﻿// ******************************************************************************************************
//  PdatSettings.tsx - Gbtc
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
//  05/03/2021 - C. Lackner
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
    /// The Settings for a <see cref="PdatImporter"/> Data Source
    /// </summary>
    public class PdatSettings
    {
        [DefaultValue("C:\\Users\\clackner\\Desktop\\Adapt")]
        [CustomConfigurationEditor("GemstoneWPF.dll", "GemstoneWPF.Editors.FolderBrowser", "showNewFolderButton=true; description=Select Root Folder")]
        public string RootFolder { get; set; }
        [DefaultValue(100)]
        [CustomConfigurationEditor("ADAPT.dll", "Adapt.View.Common.hengtest", "showNewFolderButton=true; description=test heng")]

        public int MyTestProperty { get; set; }
    }
}
