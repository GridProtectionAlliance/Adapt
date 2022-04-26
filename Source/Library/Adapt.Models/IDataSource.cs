// ******************************************************************************************************
//  IDataSource.tsx - Gbtc
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
//  03/25/2020 - C. Lackner
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
    /// Interface for an ADAPT DataSource
    /// </summary>
    public interface IDataSource: IAdapter
    {

        /// <summary>
        /// Tests the dataSource with the Current Settings
        /// </summary>
        /// <returns>Returns <see cref="true"/> if the datasource is working properly and data was found.</returns>
        public bool Test();

        /// <summary>
        /// Provides a List of <see cref="AdaptDevice"/> that are available through this Datasource.
        /// </summary>
        /// <returns>Returns a list of <see cref="AdaptDevice"/></returns>
        public IEnumerable<AdaptDevice> GetDevices();

        /// <summary>
        /// Provides a List of <see cref="AdaptSignal"/> that are available through this Datasource.
        /// </summary>
        /// <returns>Returns a list of <see cref="AdaptSignal"/></returns>
        public IEnumerable<AdaptSignal> GetSignals();

        /// <summary>
        /// Obtains data from Source system
        /// </summary>
        /// <param name="signals">The <see cref="AdaptSignal"/> list that should be obtained. </param>
        /// <param name="start"> The start</param>
        /// <param name="end">The End</param>
        /// <returns>A <see cref="IEnumerable{T}"/> of <see cref="IFrame"/> with the last Frame being at <see cref="end"/>. </returns>
        public IAsyncEnumerable<IFrame> GetData(List<AdaptSignal> signals, DateTime start, DateTime end);

        /// <summary>
        /// Indicates if The DataSource reports Progress of the current Query
        /// </summary>
        public bool SupportProgress { get; }

        /// <summary>
        /// Gets the current Progress in Percent if <see cref="SupportProgress"/> returns <see cref="true"/>
        /// </summary>
        public double GetProgress();

        /// <summary>
        /// An Event that will pass a Message to the UI
        /// </summary>
        public event EventHandler<MessageArgs> MessageRecieved;

    }
}
