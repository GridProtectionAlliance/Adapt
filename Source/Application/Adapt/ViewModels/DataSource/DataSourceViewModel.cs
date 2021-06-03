// ******************************************************************************************************
//  DataSourceViewModel.tsx - Gbtc
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
//  03/29/2020 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using Adapt.Models;
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
    /// ViewModel for DataSource Window
    /// </summary>
    public class DataSourceViewModel : ViewModelBase, IViewModel
    {
        #region [ Members ]

        private DataSource m_dataSource;
        private RelayCommand m_saveCommand;
        private RelayCommand m_deleteCommand;
        private RelayCommand m_clearCommand;

        private bool m_isTested;
        private bool m_passedTest;

        private List<DeviceViewModel> m_Devices;

        private List<DataSourceTypeDescription> m_dataSourceTypes;
        private IDataSource m_instance;
        private List<AdapterSettingParameterVM> m_settings;

        #endregion
        #region[ Properties ]

        public DataSource DataSource
        {
            get { return m_dataSource; }
            set
            {
                m_dataSource = value;
                
                OnDataSourceChanged();
            }
        }

        public string Name
        {
            get => m_dataSource?.Name ?? "";
            set
            {
                m_dataSource.Name = value;
                OnSettingsChanged();
                OnPropertyChanged();
            }
        }

        public int ID
        {
            get => m_dataSource?.ID ?? -1;
            set
            {
                if (value != (m_dataSource?.ID ?? -1))
                {
                    Load(value);
                    OnPropertyChanged();
                }
            }
        }

        public string TypeName
        {
            get => m_dataSource?.TypeName ?? "";
            set
            {
                m_dataSource.TypeName = value;
                OnSettingsChanged();
                OnPropertyChanged();
            }
        }

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

        /// <summary>
        /// Gets or sets the DataSource Instance used by this <see cref="DataSource"/>
        /// </summary>
        public IDataSource Instance
        {
            get => m_instance;
            set 
            {
                m_instance = value;
                OnInstanceChanged();
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
        
        /// <summary>
        /// Gets or sets the Settings associated with this <see cref="Instance"/>
        /// </summary>
        public List<AdapterSettingParameterVM> Settings
        {
            get => m_settings;
            set
            {
                m_settings = value;
                OnPropertyChanged();
            }
        }
        public ICommand SaveCommand => m_saveCommand;

        public ICommand DeleteCommand => m_deleteCommand;

        public ICommand ClearCommand => m_clearCommand;

        public bool CanSave => ValidConfig();

        /// <summary>
        /// Indicates if the Data source is saved and has been succesfully tested
        /// </summary>
        public bool PassedTest
        {
            get { return m_isTested && m_passedTest; }
            set { 
                m_passedTest = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FailedTest));
            }
        }

        /// <summary>
        /// Indicates if the Data source is saved and has been tested but failed
        /// </summary>
        public bool FailedTest => m_isTested && !m_passedTest;
        

        public bool CanDelete => false;

        public bool CanClear => false;
        

        public event CancelEventHandler BeforeLoad;
        public event EventHandler Loaded;
        public event CancelEventHandler BeforeSave;
        public event EventHandler Saved;
        public event CancelEventHandler BeforeDelete;
        public event EventHandler Deleted;

        public int NumberPMU => m_Devices?.Count() ?? 0;
        public int NumberSignals => m_Devices?.Sum(pmu => pmu.NSignals) ?? 0;

        public List<DeviceViewModel> Devices => m_Devices;

        #endregion

        #region [ Constructor ]
        public DataSourceViewModel(int ID): this() 
        {
            Load(ID);
        }

        public DataSourceViewModel()
        {
            m_dataSource = null;
            m_settings = new List<AdapterSettingParameterVM>();

            m_clearCommand = new RelayCommand(Clear, () => CanClear);
            m_saveCommand = new RelayCommand(Save, () => CanSave);
            m_deleteCommand = new RelayCommand(Delete, () => CanDelete);

            m_dataSourceTypes = DataSourceTypeDescription.LoadDataSourceTypes(FilePath.GetAbsolutePath("").EnsureEnd(Path.DirectorySeparatorChar));

            m_Devices = new List<DeviceViewModel>();
            
        }

        #endregion

        #region [ Methods ]
        public void Save()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                if (OnBeforeSaveCanceled())
                    throw new OperationCanceledException("Save was canceled.");

                m_dataSource.ConnectionString = AdapterSettingParameterVM.GetConnectionString(m_settings,m_instance);
                m_dataSource.AssemblyName = m_dataSourceTypes[AdapterTypeSelectedIndex].Type.Assembly.FullName;
                m_dataSource.TypeName = m_dataSourceTypes[AdapterTypeSelectedIndex].Type.FullName;

                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<DataSource>(connection).AddNewOrUpdateRecord(m_dataSource);

                Load(m_dataSource.ID);
                OnSaved();
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    Popup(ex.Message + Environment.NewLine + "Inner Exception: " + ex.InnerException.Message, "Save DataSource Exception:", MessageBoxImage.Error);
                }
                else
                {
                    Popup(ex.Message, "Save DataSource Exception:", MessageBoxImage.Error);
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
            Test();
        }

        public void Test()
        {
            m_isTested = true;

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                ConfigureInstance();

                PassedTest = m_instance.Test();
                if (m_isTested)
                    Popup("This DataSource is set up Properly and ADAPT was able to get data.", "Success", MessageBoxImage.Information);
                else
                    Popup("This DataSource is not set up Properly and ADAPT was unable to get data. Please check the settings.","Failed", MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                PassedTest = false;
                if (ex.InnerException != null)
                {
                    Popup(ex.Message + Environment.NewLine + "Inner Exception: " + ex.InnerException.Message, "Test DataSource Exception:", MessageBoxImage.Error);
                }
                else
                {
                    Popup(ex.Message, "Test DataSource Exception:", MessageBoxImage.Error);
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

        }

        private bool ValidConfig()
        {
            if (m_instance == null || m_dataSource == null)
                return false;
            if (m_settings.Any(item => item.IsRequired && item.Value is null))
                return false;
            return true;
        }

        public void Delete() 
        {}

        public void Clear()
        {}

        public void Load() => Load(m_dataSource?.ID ?? 1);
        
        public void Load(int ID)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                if (OnBeforeLoadCanceled())
                    throw new OperationCanceledException("Load was canceled.");

                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    DataSource = new TableOperations<DataSource>(connection).QueryRecordWhere("ID = {0}", ID);

                CreateInstance();
                OnLoaded();
            }
            catch(Exception ex)
            {
                if (ex.InnerException != null)
                {
                    Popup(ex.Message + Environment.NewLine + "Inner Exception: " + ex.InnerException.Message, "Load DataSource Exception:", MessageBoxImage.Error);
                }
                else
                {
                    Popup(ex.Message, "Load DataSource Exception:", MessageBoxImage.Error);
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Raises the <see cref="BeforeLoad"/> event.
        /// </summary>
        private bool OnBeforeLoadCanceled()
        {
            CancelEventArgs cancelEventArgs = new CancelEventArgs();

            if (BeforeLoad != null)
                BeforeLoad(this, cancelEventArgs);

            return cancelEventArgs.Cancel;
        }

        /// <summary>
        /// Raises the <see cref="Loaded"/> event.
        /// </summary>
        private void OnLoaded()
        {
            if (Loaded != null)
                Loaded(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="BeforeSave"/> event.
        /// </summary>
        private bool OnBeforeSaveCanceled()
        {
            CancelEventArgs cancelEventArgs = new CancelEventArgs();

            if (BeforeSave != null)
                BeforeSave(this, cancelEventArgs);

            return cancelEventArgs.Cancel;
        }

        /// <summary>
        /// Raises the <see cref="Saved"/> event.
        /// </summary>
        private void OnSaved()
        {
            if (Saved != null)
                Saved(this, EventArgs.Empty);
        }

       
        private void OnDataSourceChanged()
        {
            OnPropertyChanged(nameof(DataSource));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(ID));
            OnPropertyChanged(nameof(TypeName));
            OnPropertyChanged(nameof(Settings));
            OnPropertyChanged(nameof(AdapterTypeSelectedIndex));
        }

        private void OnAdapterTypeSelectedIndexChanged()
        {
            OnPropertyChanged(nameof(AdapterTypeSelectedIndex));
            OnPropertyChanged(nameof(TypeName));
            OnPropertyChanged(nameof(Settings));
            CreateInstance();

        }

        private void OnInstanceChanged()
        {

            OnPropertyChanged(nameof(Instance));
            if (m_instance != null)
            {
                m_settings = AdapterSettingParameterVM.GetSettingParameters(m_instance, m_dataSource.ConnectionString);
                m_settings.ForEach(s => s.SettingChanged += (object sender, SettingChangedArg arg) => OnSettingsChanged());
                ConfigureInstance();
            }
            else
                m_settings = new List<AdapterSettingParameterVM>();
            OnPropertyChanged(nameof(Settings));
        }

        
        private void OnSettingsChanged()
        {
            m_isTested = false;
            OnPropertyChanged(nameof(PassedTest));
            OnPropertyChanged(nameof(FailedTest));
        }
        /// <summary>
        /// Creates an Instance of <see cref="IDataSource"/> corresponding to the <see cref="DataSource"/>
        /// </summary>
        private void CreateInstance()
        {
            m_isTested = false;
            OnPropertyChanged(nameof(PassedTest));
            OnPropertyChanged(nameof(FailedTest));

            if (AdapterTypeSelectedIndex >= 0 && AdapterTypeSelectedIndex < m_dataSourceTypes.Count)
                Instance = (IDataSource)Activator.CreateInstance(m_dataSourceTypes[AdapterTypeSelectedIndex].Type);
               
            else
            {
                m_settings = new List<AdapterSettingParameterVM>();
                OnPropertyChanged(nameof(Settings));
            }


        }

        /// <summary>
        /// Configures the Instance of <see cref="IDataSource"/>.
        /// </summary>
        private void ConfigureInstance()
        {
            if (m_instance != null)
            {
                IConfiguration config = new ConfigurationBuilder().AddGemstoneConnectionString(AdapterSettingParameterVM.GetConnectionString(m_settings, m_instance)).Build();
                Instance.Configure(config);
                IEnumerable<AdaptSignal> signals = Instance.GetSignals();
                m_Devices = Instance.GetDevices().Select(item => new DeviceViewModel(item, signals.Where(s => item.ID == s.Device), m_dataSource.ID)).ToList();

            }
            else
                m_Devices = new List<DeviceViewModel>();
            OnPropertyChanged(nameof(Devices));
            OnPropertyChanged(nameof(NumberPMU));
            OnPropertyChanged(nameof(NumberSignals));

        }
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        
        #endregion
    }
}