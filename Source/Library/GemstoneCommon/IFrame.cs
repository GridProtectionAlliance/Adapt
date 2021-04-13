﻿//******************************************************************************************************
//  IFrame.cs - Gbtc
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
//  09/02/2010 - J. Ritchie Carroll
//       Generated original version of source code.
//  04/14/2011 - J. Ritchie Carroll
//       Added received and published timestamps for measurements.
//  05/11/2011 - J. Ritchie Carroll
//       Changed IFrame to require a concurrent dictionary.
//  12/20/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//  04/05/2021 - C. Lackner
//       Adjusted IFrame to use in adapt
//
//******************************************************************************************************

using Gemstone;
using System;
using System.Collections.Concurrent;

namespace GemstoneCommon
{
   
        /// <summary>
        /// Abstract frame interface representing a collection of measurements at an exact moment in time.
        /// </summary>
        public interface IFrame
        {
            /// <summary>
            /// Keyed measurements in this <see cref="IFrame"/>.
            /// </summary>
            /// <remarks>
            /// Represents a concurrent dictionary of measurements, keyed by <see cref="MeasurementKey"/>.
            /// </remarks>
            ConcurrentDictionary<string, ITimeSeriesValue> Measurements
            {
                get;
            }

            /// <summary>
            /// Gets or sets published state of this <see cref="IFrame"/> (pre-processing).
            /// </summary>
            bool Published
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets total number of measurements that have been sorted into this <see cref="IFrame"/>.
            /// </summary>
            /// <remarks>
            /// If this property has not been assigned a value, implementers should return measurement count.
            /// </remarks>
            int SortedMeasurements
            {
                get;
            }

            /// <summary>
            /// Gets or sets exact timestamp, in <see cref="Ticks"/>, of the data represented in this <see cref="IFrame"/>.
            /// </summary>
            /// <remarks>
            /// The value of this property represents the number of 100-nanosecond intervals that have elapsed since 12:00:00 midnight, January 1, 0001.
            /// </remarks>
            Ticks Timestamp
            {
                get;
                set;
            }


        }
    
}