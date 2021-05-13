// ******************************************************************************************************
//  MainVisualizationViewModel.tsx - Gbtc
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
    public class MainVisualizationVM : ViewModelBase
    {
        #region [ Members ]
        private DateTime m_startAvailable;
        private DateTime m_endAvailable;
        private DateTime m_startVisualization;
        private DateTime m_endVisualization;

        private List<SignalReader> m_reader;
        private LineChartVM m_lineChart;
        #endregion

        #region[ Properties ]

        /// <summary>
        /// Start of Data availability - This is dependent on the Task/Timeframe selected
        /// </summary>
        public DateTime DataAvailabilityStart
        {
            get => m_startAvailable;
        }

        /// <summary>
        /// End of Data availability - This is dependent on the Task/Timeframe selected
        /// </summary>
        public DateTime DataAvailabilityEnd
        {
            get => m_endAvailable;
        }

        /// <summary>
        /// Start of the Currently visualized Data
        /// </summary>
        public DateTime VisualizationStart
        {
            get => m_startVisualization;
        }

        /// <summary>
        /// End of the Currently visualized Data
        /// </summary>
        public DateTime VisializationEnd
        {
            get => m_endVisualization;
        }

        /// <summary>
        /// Number of loaded <see cref="SignalReader"/>
        /// </summary>
        public int Nreaders
        {
            get => m_reader.Count();
        }

        /// <summary>
        /// Note that this will need to change to an Interface to support different Plot Types down the road.
        /// </summary>
        public LineChartVM LineChart
        {
            get { return m_lineChart; }
            set
            {
                m_lineChart = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region[ Constructor]
        public MainVisualizationVM(DateTime start, DateTime end)
        {
            m_startAvailable = start;
            m_endAvailable = end;
            m_startVisualization = start;
            m_endVisualization = end;

            m_reader = SignalReader.GetAvailableReader();
            if (m_reader.Count > 0)
                LineChart = new LineChartVM(m_reader.First(), start, end);
        }

        #endregion

        #region [ Methods ]
       
        #endregion
    }

  
}