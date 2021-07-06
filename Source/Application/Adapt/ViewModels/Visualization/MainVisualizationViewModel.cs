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
using System.ComponentModel;
using System.IO;
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
        private List<IDisplayWidget> m_widgets;

        private List<Tuple<Type, string>> m_loadedWigets;
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
        /// List of Widgets to be displayed
        /// </summary>
        public List<IDisplayWidget> Widgets
        {
            get { return m_widgets; }
        }

        #endregion

        #region[ Constructor]
        public MainVisualizationVM(DateTime start, DateTime end)
        {
            m_startAvailable = start;
            m_endAvailable = end;
            m_startVisualization = start;
            m_endVisualization = end;

            m_loadedWigets = new List<Tuple<Type, string>>();
            LoadWidgets();

            m_reader = SignalReader.GetAvailableReader();

            // Temporary Setup is Linechart followed by Table.
            // meant for testing and development only.
            if (m_reader.Count > 0)
                m_widgets = new List<IDisplayWidget>() {
                    new LineChartVM(),
                    new StatisticsTableVM()
                };
            else
                m_widgets = new List<IDisplayWidget>();

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
            
            m_loadedWigets = typeof(IDisplayWidget).LoadImplementations(FilePath.GetAbsolutePath("").EnsureEnd(Path.DirectorySeparatorChar), true)
                .Distinct()
                .Where(type => GetEditorBrowsableState(type) == EditorBrowsableState.Always)
                .Select( type => new Tuple<Type,string>(type,GetDescription(type)))
                .OrderByDescending(pair => pair.Item2)
                .ToList();
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