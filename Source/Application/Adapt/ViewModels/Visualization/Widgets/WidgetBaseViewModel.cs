// ******************************************************************************************************
//  WidgetBaseViewModel.tsx - Gbtc
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
//  05/13/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using AdaptLogic;
using GemstoneCommon;
using GemstoneWPF;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adapt.ViewModels.Visualization.Widgets
{
    public abstract class WidgetBaseVM: ViewModelBase
    {

        protected SignalReader m_reader;
       

        #region [ constructor ]
        public WidgetBaseVM(SignalReader reader, DateTime start, DateTime end)
        {
            m_reader = reader;
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Zooms in to a new Section.
        /// </summary>
        /// <param name="start">new Start Time.</param>
        /// <param name="end">new End Time.</param>
        public virtual void Zoom(DateTime start, DateTime end)
        {

        }
        #endregion

    }
}
