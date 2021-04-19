// ******************************************************************************************************
//  ProgressEventArgs.tsx - Gbtc
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
//  04/13/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using System;

namespace Adapt.Models
{
    /// <summary>
    /// Arguments for a Progress Change Event
    /// </summary>
    public class ProgressArgs: EventArgs
    {
        /// <summary>
        /// Gets the Progress in Percent.
        /// </summary>
        public int Progress { get; }

        /// <summary>
        /// A Flag indicating whether this is the last Progress update and the task is complete.
        /// </summary>
        public bool Complete { get; }

        /// <summary>
        /// The Message to be displayed 
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Creates a new <see cref="ProgressArgs"/>
        /// </summary>
        /// <param name="Message">The Message to be displayed.</param>
        /// <param name="Complete"><see cref="true"/> if this is the last progress report and the Task is complete</param>
        /// <param name="Progress"> The Progress in Percent.</param>
        public ProgressArgs(string Message, bool Complete, int Progress)
        {
            this.Progress = Progress;
            this.Message = Message;
            this.Complete = Complete;

            if (Complete)
                this.Progress = 100;
        }
    }
}
