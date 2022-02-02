// ******************************************************************************************************
//  TemplateInputDevice.tsx - Gbtc
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
//  06/29/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Gemstone;
using Gemstone.Data.Model;
using GemstoneCommon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Adapt.Models
{
    /// <summary>
    /// Represents an Input Device for a Template.
    /// </summary>
    public class TemplateInputDevice
    {
        [PrimaryKey(true)]
        public int ID { get; set; } 

        public int TemplateID { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// Indicates if a Device is an Input Device or simply a virtual device
        /// </summary>
        public bool IsInput { get; set; }

        /// <summary>
        /// The name used for this PMU when creating Outputs - Accepts {PMUName.SignalName} as substitutions 
        /// </summary>
        public string OutputName { get; set; }
    
    } 
}
