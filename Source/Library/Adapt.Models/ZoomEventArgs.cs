// ******************************************************************************************************
//  ZoomEventArgs.tsx - Gbtc
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
//  05/24/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using System;
using System.Windows;

namespace Adapt.Models
{
    /// <summary>
    /// Arguments for a Progress Change Event
    /// </summary>
    public class ZoomEventArgs : EventArgs
    {


        /// <summary>
        /// The start Time of the new Window.
        /// </summary>
        public DateTime Start { get; }

        /// <summary>
        /// The end Time of the new Window.
        /// </summary>
        public DateTime End { get; }

        /// <summary>
        /// Creates a new <see cref="ZoomEventArgs"/>
        /// </summary>
        /// <param name="start"> start Time of the new window.</param>
        /// <param name="end"> end time of the new window. </param>
        public ZoomEventArgs(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

    }
}
