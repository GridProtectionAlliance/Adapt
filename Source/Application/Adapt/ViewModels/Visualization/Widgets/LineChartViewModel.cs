// ******************************************************************************************************
//  LineChartViewModel.tsx - Gbtc
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
//  04/19/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using AdaptLogic;
using GemstoneWPF;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adapt.ViewModels.Visualization.Widgets
{
    public class LineChartVM: ViewModelBase
    {
        private PlotModel m_plotModel;
        private SignalReader m_reader;
        public PlotModel PlotModel
        {
            get { return m_plotModel; }
            set
            {
                m_plotModel = value;
                OnPropertyChanged(); 
            }
        }

        #region [ constructor ]
        public LineChartVM(SignalReader reader, DateTime start, DateTime end)
        {
            m_reader = reader;

            m_plotModel = new PlotModel();
            m_plotModel.Title = "Line Chart";
            LineSeries series = new LineSeries();
            series.Points.AddRange(m_reader.GetTrend(start,end).Select(item => new DataPoint(item.Timestamp.Value, item.Value)));
            m_plotModel.Series.Add(series);
            OnPropertyChanged(nameof(PlotModel));
        }
        #endregion

    }
}
