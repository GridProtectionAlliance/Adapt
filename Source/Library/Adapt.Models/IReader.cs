// ******************************************************************************************************
//  IReader.tsx - Gbtc
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
//  05/23/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using GemstoneCommon;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Adapt.Models
{
    /// <summary>
    /// Interface for a Data Reader.
    /// </summary>
    public interface IReader
    {
        /// <summary>
        /// Gets an avg Trend Series to specified resolution.
        /// </summary>
        /// <param name="start">the start Time of the Series.</param>
        /// <param name="end">The end time of the Series.</param>
        /// <param name="points"> The minimum number of points requested.</param>
        /// <returns></returns>
        public IEnumerable<ITimeSeriesValue> GetTrend(DateTime start, DateTime end, int points);

        // <summary>
        /// Gets the Summary of this Signal for a specified Time range.
        /// </summary>
        /// <param name="start">The startTime.</param>
        /// <param name="end">The endTime.</param>
        /// <returns>An <see cref="AdaptPoint"/> specifying Min, Max and Avg</returns>
        public AdaptPoint GetStatistics(DateTime start, DateTime end);

        /// <summary>
        /// The Guid used to identify the specific Signal.
        /// This Guid is unique when looking at current Results.
        /// </summary>
        public string SignalGuid { get; }

        /// <summary>
        /// Gets the <see cref="AdaptSignal"/> This reader is attached to
        /// </summary>
        public AdaptSignal Signal { get; }

        public IEnumerable<GraphPoint> GetRangeTrend(DateTime start, DateTime end, int points);

    }
}
