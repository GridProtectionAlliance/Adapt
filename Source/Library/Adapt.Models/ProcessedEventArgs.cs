// ******************************************************************************************************
//  ProcessedEventArgs.tsx - Gbtc
//
//  Copyright © 2022, Grid Protection Alliance.  All Rights Reserved.
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
//  11/12/2022 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using System;

namespace Adapt.Models
{
    /// <summary>
    /// Arguments for a Processed Change Event
    /// </summary>
    public class ProcessedEventArgs: EventArgs
    {
        /// <summary>
        /// Gets the Number of Frames Processed.
        /// </summary>
        public int NProcessed { get; }

        
        /// <summary>
        /// The Message to be displayed 
        /// </summary>
        public DateTime TProcessed { get; }

        /// <summary>
        /// Creates a new <see cref="ProcessedEventArgs"/>
        /// </summary>
        /// <param name="N">The number of Frames Processed.</param>
        /// <param name="T"> The last Timestamp Processed.</param>
        public ProcessedEventArgs(int N, DateTime T)
        {
            this.NProcessed = N;
            this.TProcessed = T;
        }
    }
}
