// ******************************************************************************************************
//  AdaptTaskGuide.tsx - Gbtc
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
    /// A Class describing signal routings for multiple Tasks
    /// </summary>
    public class TaskRouter
    {
        #region [ Members ]

        #endregion

        #region [ Properties ]
        public DataSource DataSource { get;  }
        public List<string> InputSignalIDs { get;  }
        public List<AdaptSignal> OutputSignals { get; }
        public DateTime Start { get; }
        public DateTime End { get; }
        public List<SectionRouter> Sections { get; }

        #endregion

        #region [ Constructor ]
        public TaskRouter (DataSource dataSource, DateTime start, DateTime end, List<SectionRouter> sections)
        {
            DataSource = dataSource;
            Start = start;
            End = end;
            Sections = sections;

        }
        #endregion

        #region [ Methods ]


        #endregion
    }

    public class SectionRouter
    {
        public List<AnalyticRouter> Analytics { get; set; }
        public List<string> InputSignals { get; }
        public List<string> OuputSignals { get; }

        public SectionRouter (List<string> inputs, List<string> outputs)
        {
            InputSignals = inputs;
            OuputSignals = outputs;
        }
    }

    public class AnalyticRouter
    {
        public Type AdapterType { get; set; }
        public List<string> Inputs { get; set; }
        public IConfiguration Configuration { get; set; }
        public int FramesPerSecond { get; set; }
    }

}
