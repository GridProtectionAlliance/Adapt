﻿//******************************************************************************************************
//  IAnalogValue.cs - Gbtc
//
//  Copyright © 2012, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  02/18/2005 - J. Ritchie Carroll
//       Generated original version of source code.
//  09/15/2009 - Stephen C. Wills
//       Added new header and license agreement.
//  10/5/2012 - Gavin E. Holden
//       Added new header and license agreement.
//  12/17/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//  04/26/2021 - C. Lackner
//       moved to .net core for ADAPT.
//
//******************************************************************************************************

using Gemstone;
using Gemstone.IO.Parsing;
using Gemstone.Units;
using GemstoneCommon;
using System;
using System.Collections.Generic;

namespace GemstonePhasorProtocolls
{
    /// <summary>
    /// Represents a protocol independent interface representation of an analog value.
    /// </summary>
    public interface IAnalogValue : IChannelValue<IAnalogDefinition>
    {
        /// <summary>
        /// Gets or sets the floating point value that represents this <see cref="IAnalogValue"/>.
        /// </summary>
        double Value { get; set; }

        /// <summary>
        /// Gets or sets the integer representation of this <see cref="IAnalogValue"/> value.
        /// </summary>
        int IntegerValue { get; set; }
    }
}
