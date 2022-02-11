// ******************************************************************************************************
//  StatisticsTableViewModel.tsx - Gbtc
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
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Adapt.ViewModels.Visualization.Widgets
{
    [Description("Statistics Overview Table")]
    public class StatisticsTableVM : WidgetBaseVM, IDisplayWidget
    {
        #region [ Member ]
        private DataTable m_data;
        private UIElement m_xamlClass;

        #endregion

        #region [ Properties ]
        public DataTable DataTable
        {
            get { return m_data; }
        }

        public override UIElement UserControl => m_xamlClass;
        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates a new <see cref="StatisticsTableVM"/> that is empty.
        /// </summary>
        public StatisticsTableVM(): base()
        {
            m_xamlClass = new StatisticsTable();
            m_data = new DataTable();         

        }

        #endregion

        #region [ Methods ]

        public override void Zoom(DateTime start, DateTime end)
        {
            base.Zoom(start, end);
            UpdateTable();
        }

        public override void AddReader(IReader reader)
        {
            base.AddReader(reader);
            UpdateTable();
        }

        public override void RemoveReader(IReader reader)
        {
            base.RemoveReader(reader);
            UpdateTable();
        }

        private void UpdateTable()
        {
            m_data = new DataTable();
            m_data.Clear();
            m_data.Columns.Add("Signal");
            m_data.Columns.Add("Minimum");
            m_data.Columns.Add("Average");
            m_data.Columns.Add("Maximum");
            m_data.Columns.Add("StandardDeviation");
            m_data.Columns.Add("DataAvailability");

            foreach (IReader reader in m_readers)
            {
                AdaptPoint pt = reader.GetStatistics(m_start, m_end);
                DataRow r = m_data.NewRow();
                r["Signal"] = reader.Signal.Name;
                r["Minimum"] = pt.Min;
                r["Average"] = pt.Value;
                r["Maximum"] = pt.Max;
                r["StandardDeviation"] = pt.StandardDeviation;
                r["DataAvailability"] = pt.DataAvailability * 100.0;

                m_data.Rows.Add(r);
            }

            OnPropertyChanged(nameof(DataTable));
        }

        public override bool AllowSignal(AdaptSignal signal) 
        {
            return signal.Type != MeasurementType.EventFlag;
        }

        #endregion
    }
}
