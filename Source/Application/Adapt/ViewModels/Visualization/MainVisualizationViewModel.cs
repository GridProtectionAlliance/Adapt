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
using Gemstone.IO;
using Gemstone.Reflection.MemberInfoExtensions;
using Gemstone.StringExtensions;
using Gemstone.TypeExtensions;
using GemstoneWPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

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
        private ObservableCollection<IDisplayWidget> m_widgets;

        private Dictionary<string, Type> m_loadedWigets;
        private RelayCommand m_addWidgetCmd;
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
        /// Command to Add Widget
        /// </summary>
        public ICommand AddWidgetCommand => m_addWidgetCmd;

        /// <summary>
        /// List of Widgets to be displayed
        /// </summary>
        public ObservableCollection<IDisplayWidget> Widgets
        {
            get { return m_widgets; }
        }

        /// <summary>
        /// List of Widgets available for use
        /// </summary>
        public List<string> AvailableWidgets
        {
            get { return m_loadedWigets.Keys.ToList(); }
        }
        #endregion

        #region[ Constructor]
        public MainVisualizationVM(DateTime start, DateTime end)
        {
            m_startAvailable = start;
            m_endAvailable = end;
            m_startVisualization = start;
            m_endVisualization = end;

            m_loadedWigets = new Dictionary<string, Type>();
            LoadWidgets();

            m_addWidgetCmd = new RelayCommand( AddWidget, (p) => true);

            m_reader = SignalReader.GetAvailableReader();

            // Temporary Setup is Linechart followed by Table.
            // meant for testing and development only.
            if (m_reader.Count > 0)
                m_widgets = new ObservableCollection<IDisplayWidget>() {
                    new LineChartVM(),
                    new StatisticsTableVM()
                };
            else
                m_widgets = new ObservableCollection<IDisplayWidget>();

            InitalizeCharts();
            
        }

        #endregion

        #region [ Methods ]
        
        private void InitalizeCharts()
        {
            foreach(IDisplayWidget widget in m_widgets)
            {
                widget.Zoom(m_startAvailable, m_endAvailable);
                if (m_reader.Count > 0)
                    widget.AddReader(m_reader[0]);
                widget.ChangedWindow += ChangedWindow;
            }

            OnPropertyChanged(nameof(Widgets));
        }

        private void ChangedWindow(object sender, ZoomEventArgs args)
        {
            foreach (IDisplayWidget widget in m_widgets)
                widget.Zoom(args.Start, args.End);
        }

        private void LoadWidgets()
        {

            m_loadedWigets = typeof(IDisplayWidget).LoadImplementations(FilePath.GetAbsolutePath("").EnsureEnd(Path.DirectorySeparatorChar), true, false)
                .Distinct()
                .Where(type => GetEditorBrowsableState(type) == EditorBrowsableState.Always)
                .ToDictionary(item => GetDescription(item));

            OnPropertyChanged(nameof(AvailableWidgets));
        }

        public void AddWidget(object Description)
        {
            try
            {
                string desc = (string)Description;
                if (m_loadedWigets.ContainsKey(desc))
                {
                    m_widgets.Add((IDisplayWidget)Activator.CreateInstance(m_loadedWigets[desc]));
                    m_widgets.Last().Zoom(m_startAvailable, m_endAvailable);
                    if (m_reader.Count > 0)
                        m_widgets.Last().AddReader(m_reader[0]);
                    m_widgets.Last().ChangedWindow += ChangedWindow;
                    OnPropertyChanged(nameof(Widgets));
                }
                else
                    Popup("Unable to find widget", "Load Widget Exception:", MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Popup(ex.Message, "Load Widget Exception:", MessageBoxImage.Error);
            }
        }
        #endregion

        #region [ static ]
        private static EditorBrowsableState GetEditorBrowsableState(Type type)
        {
            EditorBrowsableAttribute editorBrowsableAttribute;

            if (type.TryGetAttribute(out editorBrowsableAttribute))
                return editorBrowsableAttribute.State;

            return EditorBrowsableState.Always;
        }

        private static string GetDescription(Type type)
        {
            DescriptionAttribute descriptionAttribute;
            string description;

            if (type.TryGetAttribute(out descriptionAttribute))
                description = descriptionAttribute.Description.ToNonNullNorEmptyString(type.FullName);
            else
                description =  type.FullName;

            return description;
        }
        #endregion
    }


}