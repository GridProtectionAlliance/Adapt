//******************************************************************************************************
//  ConfigurationBuilderExtension.cs - Gbtc
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
//  04/01/2021 - C. Lackner
//       Generated original version of source code.
//
//******************************************************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GemstoneCommon
{
    // <summary>
    /// Defines extensions for setting up configuration defaults for Gemstone Adapters.
    /// This should be merged into Gemstone.Configuration Eventually
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddGemstoneConnectionString(this IConfigurationBuilder builder, string ConnectionString)
        {


            IEnumerable<KeyValuePair<string, string>> connectionStringSettings = new ConnectionStringParser().ParseConnectionString(ConnectionString);
            builder.AddInMemoryCollection(connectionStringSettings);

            return builder;
        }

    }
}
