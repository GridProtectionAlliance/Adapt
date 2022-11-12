// ******************************************************************************************************
//  EventTimeLineViewModel.tsx - Gbtc
//
//  Copyright © 2022, Grid Protection Alliance.  All Rights Reserved.
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
    [Description("Event Timeline Chart")]
    public class EventTimeLineVM : WidgetBaseVM
    {
        #region [ Member ]
        private PlotModel m_plotModel;
        private UIElement m_xamlClass;
        private PlotController m_plotController;
        private Func<string, string> m_getDeviceName = null;
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
        public bool HasSignal => m_readers.Count() > 0;
        public PlotController PlotController => m_plotController;
        public override Func<string, string> GetDeviceDisplay { set { m_getDeviceName = value; } }
        public override UIElement UserControl => m_xamlClass;
        #endregion

        #region [ Constructor ]
        public EventTimeLineVM(): base()
        {
            m_xamlClass = new LineChart();
            m_plotModel = new PlotModel();
            m_plotController = new PlotController();
            m_plotModel.Title = "Event Time Line";
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

        // #ToDO Improve Performance by pulling all Points at once and combining as necessary
        private void UpdateChart()
        {
            m_plotModel = new PlotModel();
            m_plotController = new PlotController();

            m_plotModel.Title = "Event Time Line";

            DateTimeAxis tAxis = new DateTimeAxis()
            {
                Minimum = DateTimeAxis.ToDouble(m_start),
                Maximum = DateTimeAxis.ToDouble(m_end)
            };

            tAxis.AxisChanged += AxisChanged;

            CategoryAxis categoryAxis = new CategoryAxis { Position = AxisPosition.Left };

            int i = 0;

            foreach (IReader reader in m_readers)
            {
                IEnumerable<AdaptEvent> evt = reader.GetEvents(m_start, m_end);

                if (m_getDeviceName is null)
                    categoryAxis.Labels.Add(reader.Signal.Name);
                else
                    categoryAxis.Labels.Add(m_getDeviceName(reader.Signal.Device) + " - " + reader.Signal.Name);

                IntervalBarSeries s1 = new IntervalBarSeries();
                s1.Items.AddRange(evt.Select(e => new IntervalBarItem {
                    Start = DateTimeAxis.ToDouble(e.Timestamp),
                    End = DateTimeAxis.ToDouble(e.Timestamp + (long)e.Value),
                    CategoryIndex = i
                }));

                m_plotModel.Series.Add(s1);

                i++;
            }

            m_plotModel.Axes.Add(categoryAxis);
            m_plotModel.Axes.Add(tAxis);

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
            OnPropertyChanged(nameof(HasSignal));
        }

        public override void RemoveReader(IReader reader)
        {
            base.RemoveReader(reader);
            UpdateChart();
            OnPropertyChanged(nameof(HasSignal));
        }

        public override bool AllowSignal(AdaptSignal signal) 
        {
            return signal.Type == MeasurementType.EventFlag;
        }

        #endregion
    }
}
