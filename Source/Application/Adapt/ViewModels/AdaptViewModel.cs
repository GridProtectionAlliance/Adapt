// ******************************************************************************************************
//  AdaptViewModel.tsx - Gbtc
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
using GemstoneWPF;
using System.ComponentModel;

namespace Adapt.ViewModels
{
    /// <summary>
    /// Primary ViewModel for Adapt
    /// </summary>
    public class AdaptViewModel: ViewModelBase
    {
        #region [ Members ]

        private DataSourceListViewModel m_dataSourceList;
        private SelectedExpander m_currentExpander;

        private ViewModelBase m_currentView;
        private DataSourceViewModel m_dataSource;

        #endregion

        #region[ Properties ]

        public DataSourceListViewModel DataSourceList {
            get => m_dataSourceList;
            set
            {
                m_dataSourceList = value;
                OnPropertyChanged();
            }
        }

        public SelectedExpander ActiveExpander
        {
            get => m_currentExpander;
            set
            {
                m_currentExpander = value;
                OnPropertyChanged();
            }
        }

        public ViewModelBase CurrentView
        {
            get => m_currentView;
            set
            {
                m_currentView = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region[ Constructor]
        public AdaptViewModel()
        {
            m_currentExpander = SelectedExpander.DataSource;
            m_dataSourceList = new DataSourceListViewModel();

            m_dataSource = new DataSourceViewModel(m_dataSourceList.SelectedID);
            m_dataSourceList.PropertyChanged += DataSourceList_Changed;

            m_dataSource.PropertyChanged += DataSource_Changed;

            m_currentView = m_dataSource;
        }


        #endregion

        #region [ Methods ]

        public void DataSourceList_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedIndex")
            {
                if (m_dataSource.ID != m_dataSourceList.SelectedID)
                    m_dataSource.ID = m_dataSourceList.SelectedID;
            }
        }

        public void DataSource_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DataSource")
            {
                if (m_dataSource.ID != m_dataSourceList.SelectedID)
                    m_dataSourceList.Load(m_dataSource.ID);
            }
        }
        #endregion
    }

    /// <summary>
    /// Enum to indicate which Expander tab is open
    /// </summary>
    public enum SelectedExpander
    {
        None = 0,
        DataSource = 1,
        Template = 2,
        Task = 3,
        Visualization = 4
    }
       
}