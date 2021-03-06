// ******************************************************************************************************
//  IAnalytic.tsx - Gbtc
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
//  08/02/2020 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using GemstoneCommon;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Adapt.Models
{
    /// <summary>
    /// Interface for an ADAPT Analytic
    /// </summary>
    public interface IAnalytic: IAdapter
    {
        /// <summary>
        /// Returns the <see cref="Type"/> of the Settings object that defines all Parameters
        /// </summary>
        public new Type GetSettingType();

        /// <summary>
        /// Returns a List of strings indicating the names of the Output Signals
        /// </summary>
        public IEnumerable<string> OutputNames();

        /// <summary>
        /// Returns a List of strings indicating the names of the Input Signals
        /// </summary>
        public IEnumerable<string> InputNames();

    }
}
