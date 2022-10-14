//******************************************************************************************************
//  ITimeSeriesValue.cs - Gbtc
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
//  06/29/2011 - J. Ritchie Carroll
//       Generated original version of source code.
//  12/20/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//  04/01/2021 - C. Lackner
//       Moved to .NET Core.
//
//******************************************************************************************************

using Gemstone;
using System;

namespace GemstoneCommon
{
    /// <summary>
    /// Represents the interface for a time-series value.
    /// </summary>
    public interface ITimeSeriesValue
    {
        /// <summary>
        /// Gets or sets the <see cref="string"/> based signal ID of this <see cref="ITimeSeriesValue"/>.
        /// </summary>
        /// <remarks>
        /// This is the fundamental identifier of the <see cref="ITimeSeriesValue"/>.
        /// </remarks>
        string ID
        {
            get;
        }

        /// <summary>
        /// Gets or sets the Value of this <see cref="ITimeSeriesValue"/>.
        /// </summary>
        double Value
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets exact timestamp, in ticks, of the data represented by this <see cref="ITimeSeriesValue{T}"/>.
        /// </summary>
        /// <remarks>
        /// The value of this property represents the number of 100-nanosecond intervals that have elapsed since 12:00:00 midnight, January 1, 0001.
        /// </remarks>
        Ticks Timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates if a ITimeSeriesValue is an event and needs to be saved with additional Parameters
        /// </summary>
        bool IsEvent { get; }

        /// <summary>
        /// Clones the <see cref="ITimeSeriesValue"/> in a new <see cref="ITimeSeriesValue"/> with a different <see cref="ID"/>
        /// </summary>
        /// <param name="ID">The new ID</param>
        /// <returns>The cloned <see cref="ITimeSeriesValue"/></returns>
        public ITimeSeriesValue Clone(string ID);

    }
}
