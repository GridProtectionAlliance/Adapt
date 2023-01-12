// ******************************************************************************************************
//  PointListViewModel.tsx - Gbtc
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
using Adapt.ViewModels.Vizsalization;
using AdaptLogic;
using Gemstone;
using Gemstone.Collections.CollectionExtensions;
using GemstoneCommon;
using GemstoneWPF;
using Google.Protobuf.WellKnownTypes;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace Adapt.ViewModels.Visualization.Widgets
{
    [Description("List of Actual Points")]
    public class PointListVM : WidgetBaseVM, IDisplayWidget
    {
        #region [ Member ]
        private DataTable m_data;
        private UIElement m_xamlClass;
        private Func<string, string> m_getDeviceName = null;
        #endregion

        #region [ Properties ]
        public DataTable DataTable
        {
            get { return m_data; }
        }
        public bool HasSignal => m_readers.Count() > 0;
        public override UIElement UserControl => m_xamlClass;
        public override List<IContextMenu> Actions => new List<IContextMenu>() { new WidgetVM.ContextMenueVM("Export to CSV", ExportPointData) };
        public override Func<string, string> GetDeviceDisplay { set { m_getDeviceName = value; } }
        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates a new <see cref="EventListVM"/> that is empty.
        /// </summary>
        public PointListVM(): base()
        {
            m_xamlClass = new StatisticsTable();
            m_data = new DataTable();         

        }

        #endregion

        #region [ Methods ]

        public override void Zoom(DateTime start, DateTime end)
        {
            base.Zoom(start, end);
        }

        public override void AddReader(IReader reader)
        {
            base.AddReader(reader);
            UpdateTable();
            OnPropertyChanged(nameof(HasSignal));
        }

        public override void RemoveReader(IReader reader)
        {
            base.RemoveReader(reader);
            UpdateTable();
            OnPropertyChanged(nameof(HasSignal));
        }

        private void UpdateTable()
        {
            m_data = new DataTable();
            m_data.Clear();

            if ((m_end - m_start).TotalSeconds > 30)
            {
                m_data.Columns.Add("Please Select a shorter timerange. This Widget is only available for < 30 sec");
                OnPropertyChanged(nameof(DataTable));
                return;
            }

            m_data.Columns.Add("Date");
            m_data.Columns.Add("Time");
            m_data.Columns.Add("ms");
            m_data.Columns.Add("Ticks");
            Dictionary<Ticks, Dictionary<string,double>> data = new Dictionary<Ticks, Dictionary<string, double>>();
            foreach (IReader reader in m_readers)
            {
                m_data.Columns.Add(reader.Signal.Name);
                List<ITimeSeriesValue> values = reader.GetTrend(m_start, m_end, (int)Math.Ceiling(reader.Signal.FramesPerSecond * (m_end - m_start).TotalSeconds)).ToList();
                foreach (ITimeSeriesValue v in values)
                { 
                    data.AddOrUpdate(v.Timestamp,
                        (c) =>
                        {
                            Dictionary<string, double> d = new Dictionary<string, double>();
                            d.Add(reader.Signal.Name, v.Value);
                            return d;
                        },
                        (c, d) => { d.AddOrUpdate(reader.Signal.Name, v.Value); return d; });
                }
            }

            foreach (Ticks ts in data.Keys.OrderBy(s => s))
            {
                DataRow r = m_data.NewRow();
                r["Date"] = ts.ToString("MM/dd/yyyy");
                r["Time"] = ts.ToString("HH:mm:ss");
                r["ms"] = (double)(ts.DistanceBeyondSecond()/Ticks.PerMillisecond);
                r["Ticks"] = ts.Value.ToString();
                foreach (string sig in data[ts].Keys)
                    r[sig] = data[ts][sig];
                m_data.Rows.Add(r);
            }

            OnPropertyChanged(nameof(DataTable));
        }
        public override bool AllowSignal(AdaptSignal signal) => signal.Type != MeasurementType.EventFlag;

        private void ExportPointData()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            DialogResult result = saveFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                StringBuilder sbbuilder = new StringBuilder();

                IEnumerable<string> columnNames = m_data.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
                sbbuilder.AppendLine(string.Join(",", columnNames));

                foreach (DataRow row in m_data.Rows)
                {
                    IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                    sbbuilder.AppendLine(string.Join(",", fields));
                }

                File.WriteAllText(saveFileDialog.FileName, sbbuilder.ToString());
            }
        }
    }

        #endregion
    
}
