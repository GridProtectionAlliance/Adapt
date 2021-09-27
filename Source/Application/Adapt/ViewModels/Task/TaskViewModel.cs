// ******************************************************************************************************
//  TaskViewModel.tsx - Gbtc
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
//  09/07/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using Adapt.Models;
using Adapt.View.Template;
using Adapt.ViewModels.Common;
using Gemstone.Data;
using Gemstone.Data.Model;
using Gemstone.IO;
using Gemstone.StringExtensions;
using GemstoneCommon;
using GemstoneWPF;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Transactions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for Task Window
    /// </summary>
    public class TaskVM : ViewModelBase
    {
        #region [ Members ]
        private int m_SelectedTemplateIndex;
        private int m_SelectedDataSourceIndex;
        #endregion

        #region[ Properties ]

        /// <summary>
        /// Represents a List of all available DataSources.
        /// </summary>
        public ObservableCollection<DataSource> DataSources { get; set; }

        /// <summary>
        /// Represents a List of all available Templates.
        /// </summary>
        public ObservableCollection<Template> Templates { get; set; }
        public DateSelectVM TimeSelectionViewModel { get; }

        /// <summary>
        /// The index of the DataSource Selected.
        /// </summary>
        public int SelectedDataSourceIndex { 
            get => m_SelectedDataSourceIndex; 
            set
            {
                m_SelectedDataSourceIndex = value;

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The index of the Template Selected.
        /// </summary>
        public int SelectedTemplateIndex
        { 
            get => m_SelectedTemplateIndex;
            set
            {
                m_SelectedTemplateIndex = value;
                MappingViewModel = new MappingVM(Templates[m_SelectedTemplateIndex]);
                ValidateDataSource();
                
                OnPropertyChanged();
            }
        }
        //public ObservableCollection<SectionVM> Sections => m_Sections;

        /// <summary>
        /// The Viewmodel containing the mapping between Template Devices and DataSourceDevices
        /// </summary>
        public MappingVM MappingViewModel { get; set; }
        #endregion

        #region [ Constructor ]

        // #ToDo: Add logic to save Task to temporary File to avoid having to set it up every time Adapt/SciSync is opened
        public TaskVM()
        {
            
            LoadDataSources();
            SelectedDataSourceIndex = 0;

            LoadTemplates();
            m_SelectedTemplateIndex = 0;
            TimeSelectionViewModel = new DateSelectVM();

            MappingViewModel = new MappingVM(Templates[SelectedTemplateIndex]);
            ValidateDataSource();
        }

        #endregion

        #region [ Methods ]
        
        /// <summary>
        /// Loads all Templates available.
        /// </summary>
        private void LoadTemplates()
        {
            Templates = new ObservableCollection<Template>();

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                Templates = new ObservableCollection<Template>(
                    new TableOperations<Template>(connection).QueryRecords().ToList()
                    );

            OnPropertyChanged(nameof(Templates));
        }

        /// <summary>
        /// Loads all DataSources available.
        /// </summary>
        private void LoadDataSources()
        {
            DataSources = new ObservableCollection<DataSource>();

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                DataSources = new ObservableCollection<DataSource>(
                    new TableOperations<DataSource>(connection).QueryRecords().ToList()
                    );
            
            OnPropertyChanged(nameof(DataSources));
        }

        private void ValidateDataSource()
        {
            try 
            {
                IDataSource Instance = (IDataSource)Activator.CreateInstance(DataSources[m_SelectedDataSourceIndex].AssemblyName,DataSources[m_SelectedDataSourceIndex].TypeName).Unwrap();
                IConfiguration config = new ConfigurationBuilder().AddGemstoneConnectionString(DataSources[m_SelectedDataSourceIndex].ConnectionString).Build();
                Instance.Configure(config);

                if (MappingViewModel != null)
                    MappingViewModel.UpdateDataSource(Instance, DataSources[m_SelectedDataSourceIndex]);

                OnPropertyChanged(nameof(MappingVM));
            }
            catch (Exception ex)
            {
                Popup($"The Datasource could not be loaded: {ex.Message}","An Error Occurred", MessageBoxImage.Error);

            }
        }
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        
        #endregion
    }
}