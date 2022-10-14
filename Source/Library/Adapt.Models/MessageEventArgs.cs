// ******************************************************************************************************
//  MessageEventArgs.tsx - Gbtc
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
//  04/26/2022 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************


using System;

namespace Adapt.Models
{
    /// <summary>
    /// Arguments for a Message Event
    /// </summary>
    public class MessageArgs: EventArgs
    {
        public enum MessageLevel
        {
            Error,
            Info,
        }
       

        /// <summary>
        /// The Message to be displayed 
        /// </summary>
        public string Message { get; }

        public Exception ex { get; }

        /// <summary>
        /// Creates a new <see cref="ProgressArgs"/>
        /// </summary>
        /// <param name="Message">The Message to be displayed.</param>
        /// <param name="Level"> The Level of this Message.</param>
        public MessageArgs(string Message, MessageLevel Level)
        {
            this.Message = Message;
            this.ex = null;
        }

        /// <summary>
        /// Creates a new <see cref="ProgressArgs"/>
        /// </summary>
        /// <param name="Message">The Message to be displayed.</param>
        /// <param name="ex">The Exception to be Logged.</param>
        /// <param name="Level"> The Level of this Message.</param>
        public MessageArgs(string Message, Exception ex, MessageLevel Level)
        {
            this.Message = Message;
            this.ex = ex;
        }

    }
}
