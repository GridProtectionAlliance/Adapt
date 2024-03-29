﻿//******************************************************************************************************
//  PhasorValueCollection.cs - Gbtc
//
//  Copyright © 2021, Grid Protection Alliance.  All Rights Reserved.
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
//  11/12/2004 - J. Ritchie Carroll
//       Generated original version of source code.
//  09/15/2009 - Stephen C. Wills
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
using System.Runtime.Serialization;

namespace GemstonePhasorProtocolls
{
    /// <summary>
    /// Represents a protocol independent collection of <see cref="IPhasorValue"/> objects.
    /// </summary>
    [Serializable]
    public class PhasorValueCollection : ChannelValueCollectionBase<IPhasorDefinition, IPhasorValue>
    {
        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="PhasorValueCollection"/> using specified <paramref name="lastValidIndex"/>.
        /// </summary>
        /// <param name="lastValidIndex">Last valid index for the collection (i.e., maximum count - 1).</param>
        /// <remarks>
        /// <paramref name="lastValidIndex"/> is used instead of maximum count so that maximum type values may
        /// be specified as needed. For example, if the protocol specifies a collection with a signed 16-bit
        /// maximum length you can specify <see cref="short.MaxValue"/> (i.e., 32,767) as the last valid index
        /// for the collection since total number of items supported would be 32,768.
        /// </remarks>
        public PhasorValueCollection(int lastValidIndex)
            : base(lastValidIndex)
        {
        }

        /// <summary>
        /// Creates a new <see cref="PhasorValueCollection"/> from serialization parameters.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> with populated with data.</param>
        /// <param name="context">The source <see cref="StreamingContext"/> for this deserialization.</param>
        protected PhasorValueCollection(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}
