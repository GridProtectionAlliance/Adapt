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
using AdaptLogic;
using GemstoneWPF;
using System;
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
        private TemplateListVM m_templateList;
        private SelectedExpander m_currentExpander;

        private AdaptTabViewModelBase m_currentView;
        private DataSourceViewModel m_dataSource;
        private TemplateVM m_template;
        private TaskVM m_task;
        private ResultViewVM m_results;

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

        public TemplateListVM TemplateList
        {
            get => m_templateList;
            set
            {
                m_templateList = value;
                OnPropertyChanged();
            }
        }

        public SelectedExpander ActiveExpander
        {
            get => m_currentExpander;
            set
            {
                if (!m_currentView.ChangeTab())
                    return;
                m_currentExpander = value;
                TabChanged();
                OnPropertyChanged();
            }
        }

        public AdaptTabViewModelBase CurrentView
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
            m_templateList = new TemplateListVM();

            m_template = new TemplateVM(m_templateList.SelectedID);
            m_templateList.PropertyChanged += TemplateList_Changed;

            m_dataSource = new DataSourceViewModel(m_dataSourceList.SelectedID);
            m_dataSourceList.PropertyChanged += DataSourceList_Changed;
  
            m_task = new TaskVM(this);

            m_results = new ResultViewVM();

            m_dataSource.PropertyChanged += DataSource_Changed;
            m_dataSource.Deleted += DataSource_Deleted;

            m_template.PropertyChanged += Template_Changed;
            m_template.Deleted += Templated_Deleted;

            m_currentView = m_dataSource;

        }

        #endregion

        #region [ Methods ]

        public void DataSourceList_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(m_dataSourceList.SelectedIndex))
            {
                if (m_dataSource.ID != m_dataSourceList.SelectedID)
                    if (m_dataSource.ChangeTab())
                        m_dataSource.ID = m_dataSourceList.SelectedID;
                    else
                        m_dataSourceList.SelectedID = m_dataSource.ID;
            }

            if (e.PropertyName == nameof(m_dataSourceList.DataSource))
            {
                m_task.LoadDataSources();
            }
        }

        public void TemplateList_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(m_templateList.SelectedIndex))
            {
                if (m_template.ID != m_templateList.SelectedID)
                    m_template.Load(m_templateList.SelectedID);
            }

            if (e.PropertyName == nameof(m_templateList.Templates))
            {
                m_task.LoadTemplates();
            }
        }

        public void DataSource_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(m_dataSource.DataSource))
            {
                if (m_dataSource.ID != m_dataSourceList.SelectedID)
                    m_dataSourceList.Load(m_dataSource.ID);
            }

        }

        public void DataSource_Deleted(object sender, EventArgs e)
        {
            m_dataSourceList.Load();
            if (m_dataSourceList.DataSource.Count > 0)
                m_dataSource.Load(m_dataSourceList.DataSource[0].ID);
        }

        public void Templated_Deleted(object sender, EventArgs e)
        {
            
            m_templateList.Load();
            
        }


        public void Template_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(m_template.ID))
            {
                if (m_template.ID != m_templateList.SelectedID)
                    m_templateList.Load(m_template.ID);
            }
        }

        private void TabChanged()
        {
            if (m_currentExpander == SelectedExpander.DataSource)
                m_currentView = m_dataSource;
            else if (m_currentExpander == SelectedExpander.Template)
                m_currentView = m_template;
            else if (m_currentExpander == SelectedExpander.Task)
                m_currentView = m_task;
            else if (m_currentExpander == SelectedExpander.Visualization)
                m_currentView = m_results;
            OnPropertyChanged(nameof(CurrentView));
        }

        /// <summary>
        /// Start Processing a Task 
        /// </summary>
        /// <param name="task"> The <see cref="AdaptTask"/> to be processed. </param>
        public void ProcessTask(AdaptTask task)
        {
            ActiveExpander = SelectedExpander.Visualization;
            m_results.ProcessTask(task);
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