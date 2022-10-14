// ******************************************************************************************************
//  VisualizationWindowViewModel.tsx - Gbtc
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
//  03/25/2020 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using Adapt.Models;
using Adapt.ViewModels.Visualization.Widgets;
using AdaptLogic;
using GemstoneWPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Adapt.ViewModels.Vizsalization
{
    /// <summary>
    /// Primary ViewModel for <see cref="Adapt.View.Visualization.MainVisualization"/>
    /// </summary>
    public class VisualizationWindowVM : ViewModelBase
    {
        #region [ Members ]
        MainVisualizationVM m_vieModel;
        #endregion

        #region[ Properties ]


        public MainVisualizationVM VisualizationVM
        {
            get { return m_vieModel; }
            set 
            {
                m_vieModel = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region[ Constructor]
        public VisualizationWindowVM(AdaptTask Task, TaskProcessor Processor)
        {
            VisualizationVM = new MainVisualizationVM(Task, Processor);
        }

        #endregion

        #region [ Methods ]
       
        #endregion
    }

  
}