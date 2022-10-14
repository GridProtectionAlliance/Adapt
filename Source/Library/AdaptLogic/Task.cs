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
        public class TaskSection
        {
            public TemplateSection Model { get; set; }
            public List<TaskAnalytic> Analytics { get; set; }
        }

        public class TaskAnalytic 
        {
            public Analytic Model { get; set; }
            public List<AnalyticInput> InputModel { get; set; }
            public List<AnalyticOutputSignal> OutputModel { get; set; }
            public Type AnalyticType { get; set; }
            public IConfiguration Configuration { get; set; }
        }
        #endregion

        #region [ Properties ]
        public DataSource DataSourceModel { get; set; }
        public Template TemplateModel { get; set; }
        public List<TaskSection> Sections { get; set; }
        public List<TemplateInputDevice> DevicesModels { get; set; }
        public List<TemplateInputSignal> SignalModels { get; set; }
        public List<TemplateOutputSignal> OutputSignalModels { get; set; }

        public List<Dictionary<int, IDevice>> DeviceMappings { get; set; }
        public List<Dictionary<int, AdaptSignal>> SignalMappings { get; set; }

        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public int NTemplates => DeviceMappings?.Count() ?? 0;
        #endregion

        #region [ Constructor ]

        #endregion

        #region [ Methods ]


        #endregion
    }
}
