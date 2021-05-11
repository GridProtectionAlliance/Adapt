// ******************************************************************************************************
//  NewDataSourceWindowViewModel.tsx - Gbtc
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
//  05/11/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using Adapt.Models;
using Adapt.View;
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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for New DataSource Window
    /// </summary>
    public class NewDataSourceWindowVM: ViewModelBase
    {
        #region [ Members ]

        private DataSource m_dataSource;
        private RelayCommand m_saveCommand;
        private RelayCommand m_cancelCommand;

        private List<DataSourceTypeDescription> m_dataSourceTypes;
        
        #endregion

        #region[ Properties ]

        public DataSource DataSource
        {
            get { return m_dataSource; }  
        }

        public string Name
        {
            get => m_dataSource?.Name ?? "";
            set
            {
                m_dataSource.Name = value;
                OnPropertyChanged();
            }
        }

        
        public string TypeName
        {
            get => m_dataSource?.TypeName ?? "";
            set
            {
                m_dataSource.TypeName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Event that get's raised when a new Datasource has been saved
        /// </summary>
        public event EventHandler<DataSourceAddedArgs> AddedDataSource;

        /// <summary>
        /// Gets or sets the index of the selected item in the <see cref="DataSourceTypes"/>.
        /// </summary>
        public int AdapterTypeSelectedIndex
        {
            get
            {
                int index = m_dataSourceTypes
                    .Select(tuple => tuple.Type)
                    .TakeWhile(type => type.FullName != TypeName)
                    .Count();

                if (index == m_dataSourceTypes.Count)
                    index = -1;

                return index;
            }
            set
            {
                if (value >= 0 && value < m_dataSourceTypes.Count)
                    TypeName = m_dataSourceTypes[value].Type.FullName;

                OnAdapterTypeSelectedIndexChanged();
            }
        }

        
        public List<DataSourceTypeDescription> DataSourceTypes
        {
            get => m_dataSourceTypes;
            set
            {
                m_dataSourceTypes = value;
                OnPropertyChanged();
            }
        }
        
        
        public ICommand SaveCommand => m_saveCommand;

        public ICommand CancelCommand => m_cancelCommand;
       
        public bool CanSave => ValidConfig();

        #endregion

        #region [ Constructor ]
        
        public NewDataSourceWindowVM()
        {
            m_dataSource = new DataSource() 
            { 
                ID=-1,
                Name=""
            };

            m_saveCommand = new RelayCommand(Save, () => CanSave);
            m_cancelCommand = new RelayCommand(new Action<object>(Cancel), (object w) => true);
            
            m_dataSourceTypes = DataSourceTypeDescription.LoadDataSourceTypes(FilePath.GetAbsolutePath("").EnsureEnd(Path.DirectorySeparatorChar));

        }

        #endregion

        #region [ Methods ]
        public void Save()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                
                m_dataSource.ConnectionString = "";
                m_dataSource.AssemblyName = m_dataSourceTypes[AdapterTypeSelectedIndex].Type.Assembly.FullName;
                m_dataSource.TypeName = m_dataSourceTypes[AdapterTypeSelectedIndex].Type.FullName;

                int id = 0;
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                {
                    new TableOperations<DataSource>(connection).AddNewRecord(m_dataSource);
                    id = connection.ExecuteScalar<int>("Select ID FROM DataSource WHERE Name = {0}", m_dataSource.Name);
                }

                AddedDataSource?.Invoke(this, new DataSourceAddedArgs(id));

            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    Popup(ex.Message + Environment.NewLine + "Inner Exception: " + ex.InnerException.Message, "Add DataSource Exception:", MessageBoxImage.Error);
                }
                else
                {
                    Popup(ex.Message, "Add DataSource Exception:", MessageBoxImage.Error);
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        public void Cancel(object window)
        {
            ((NewDataSourceWindow)window).Close();
        }

        private bool ValidConfig()
        {
            if (m_dataSource.Name == null || m_dataSource.Name.Length == 0)
                return false;
            if (AdapterTypeSelectedIndex < 0)
                return false;

            return true;
        }

      
        private void OnAdapterTypeSelectedIndexChanged()
        {
            OnPropertyChanged(nameof(AdapterTypeSelectedIndex));
            OnPropertyChanged(nameof(TypeName));

        }

       
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        
        #endregion
    }
}