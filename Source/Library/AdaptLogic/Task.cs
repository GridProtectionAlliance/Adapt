// ******************************************************************************************************
//  Task.tsx - Gbtc
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
//  04/04/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************

using Adapt.Models;
using Gemstone;
using GemstoneCommon;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AdaptLogic
{
    /// <summary>
    /// A Task to be Processed 
    /// </summary>
    public class AdaptTask
    {
        #region [ Members ]

        #endregion

        #region [ Properties ]
        public DataSource DataSource { get; set; }
        public List<string> InputSignalIds { get; set; }

        public List<AdaptSignal> OutputSignals { get; set; }

        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public List<TaskSection> Sections { get; set; }

        public Dictionary<string, Tuple<string, string>[]> VariableReplacements { get; set; }
        #endregion

        #region [ Constructor ]

        #endregion

        #region [ Methods ]


        #endregion
    }

    public class TaskSection
    {
        public List<Analytic> Analytics { get; set; }
    }

    public class Analytic
    {
        public Type AdapterType { get; set; }
        public List<string> Inputs { get; set; }
        public List<AnalyticOutputDescriptor> Outputs { get; set; }
        public IConfiguration Configuration { get; set; }
        public int FramesPerSecond { get; set; }
    }

}
