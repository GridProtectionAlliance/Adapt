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
using Adapt.Models;
using Adapt.View.Visualization.Widgets;
using AdaptLogic;
using GemstoneCommon;
using GemstoneWPF;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Adapt.ViewModels.Visualization.Widgets
{
    [Description("Average Line Chart")]
    public class LineChartVM: WidgetBaseVM
    {
        #region [ Member ]
        private PlotModel m_plotModel;
        private UIElement m_xamlClass;
        private PlotController m_plotController;
        #endregion

        #region [ Properties ]
        public PlotModel PlotModel
        {
            get { return m_plotModel; }
            set
            {
                m_plotModel = value;
                OnPropertyChanged(); 
            }
        }

        public PlotController PlotController => m_plotController;
   
        public override UIElement UserControl => m_xamlClass;
        #endregion

        #region [ Constructor ]
        public LineChartVM(): base()
        {
            m_xamlClass = new LineChart();
            m_plotModel = new PlotModel();
            m_plotController = new PlotController();
            m_plotModel.Title = "Average Value Line Chart";
            OnPropertyChanged(nameof(PlotModel));
            OnPropertyChanged(nameof(PlotController));

        }

        #endregion

        #region [ Methods ]

        public override void Zoom(DateTime start, DateTime end)
        {
            base.Zoom(start, end);
            UpdateChart();
        }

        private void UpdateChart()
        {
            m_plotModel = new PlotModel();
            m_plotController = new PlotController();

            m_plotModel.Title = "Average Value Line Chart";
            DateTimeAxis tAxis = new DateTimeAxis()
            {
                Minimum = DateTimeAxis.ToDouble(m_start),
                Maximum = DateTimeAxis.ToDouble(m_end)
            };

            m_plotModel.Axes.Add(tAxis);

            tAxis.AxisChanged += AxisChanged;

            //m_plotController.UnbindAll();

            foreach (IReader reader in m_readers)
            {
                LineSeries series = new LineSeries();
                List<ITimeSeriesValue> lst = reader.GetTrend(m_start, m_end, 100).ToList();
                series.Points.AddRange(lst.Select(item => new DataPoint(DateTimeAxis.ToDouble(item.Timestamp), item.Value)));
                m_plotModel.Series.Add(series);
            }


            OnPropertyChanged(nameof(PlotModel));
            OnPropertyChanged(nameof(PlotController));
        }

        private void AxisChanged(object? sender, AxisChangedEventArgs args)
        {
            if (args.ChangeType == AxisChangeTypes.Reset || sender is null)
                return;
            DateTimeAxis axis = sender as DateTimeAxis;
            DateTime dtMax = DateTime.FromOADate(axis.ActualMaximum);
            DateTime dtMin = DateTime.FromOADate(axis.ActualMinimum);

            OnWindowChange(new ZoomEventArgs(dtMin,dtMax));
        }
        public override void AddReader(IReader reader)
        {
            base.AddReader(reader);
            UpdateChart();
        }

        public override void RemoveReader(IReader reader)
        {
            base.RemoveReader(reader);
            UpdateChart();
        }

        public override bool AllowSignal(AdaptSignal signal) 
        {
            return signal.Type != MeasurementType.EventFlag;
        }

        #endregion
    }
}
