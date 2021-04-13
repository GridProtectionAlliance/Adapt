﻿// ******************************************************************************************************
//  MeasurmentType.tsx - Gbtc
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
//  04/01/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using Gemstone;
using GemstoneCommon;
using System;

namespace Adapt.Models
{
    /// <summary>
    /// Represents a Measurement Type in ADAPT
    /// </summary>
    public enum MeasurementType
    {
       VoltageMagnitude,
       VoltagePhase,
       CurrentMagnitude,
       CurrentPhase,
       VoltageRMS,
       CurrentRMS,
       Frequency,
       Digital,
       Analog,
       DeltaFrequency,
       ROCOF,
       Other
    }
}
