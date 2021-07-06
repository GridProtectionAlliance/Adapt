// ******************************************************************************************************
//  TemplateViewModel.tsx - Gbtc
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
//  06/29/2020 - C. Lackner
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
using System.Data;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Windows;
using System.Windows.Input;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for DataSource Window
    /// </summary>
    public class TemplateVM : ViewModelBase, IViewModel
    {
        #region [ Members ]

        private Template m_template;
        private RelayCommand m_saveCommand;
        private RelayCommand m_deleteCommand;
        private RelayCommand m_clearCommand;
        private List<InputDeviceVM> m_Devices;

        #endregion

        #region[ Properties ]

        public Template Template
        {
            get { return m_template; }
            set
            {
                m_template = value;
                
                OnTemplateChanged();
            }
        }

        public string Name
        {
            get => m_template?.Name ?? "";
            set
            {
                m_template.Name = value;
                OnPropertyChanged();
            }
        }

        public int ID
        {
            get => m_template?.Id ?? -1;
            set
            {
                if (value != (m_template?.Id ?? -1))
                {
                    Load(value);
                    OnPropertyChanged();
                }
            }
        }

       

     

        
        public ICommand SaveCommand => m_saveCommand;

        public ICommand DeleteCommand => m_deleteCommand;

        public ICommand ClearCommand => m_clearCommand;

        public bool CanSave => true;

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

        public List<InputDeviceVM> Devices => m_Devices;

        #endregion

        #region [ Constructor ]
        public TemplateVM(int ID): this() 
        {
            Load(ID);
        }

        public TemplateVM()
        {
            m_template = null;
            m_Devices = new List<InputDeviceVM>();

            m_clearCommand = new RelayCommand(Clear, () => CanClear);
            m_saveCommand = new RelayCommand(Save, () => CanSave);
            m_deleteCommand = new RelayCommand(Delete, () => CanDelete);

            
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

             
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<Template>(connection).AddNewOrUpdateRecord(m_template);

                Load(m_template.Id);
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
                    Popup(ex.Message, "Save Template Exception:", MessageBoxImage.Error);
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

       
        public void Delete() 
        {}

        public void Clear()
        {}

        public void Load() => Load(m_template?.Id ?? 1);
        
        public void Load(int ID)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                if (OnBeforeLoadCanceled())
                    throw new OperationCanceledException("Load was canceled.");

                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    Template = new TableOperations<Template>(connection).QueryRecordWhere("Id = {0}", ID);

                LoadDevices();
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

       
        private void OnTemplateChanged()
        {
            OnPropertyChanged(nameof(Template));
            OnPropertyChanged(nameof(Name));
           
        }
        
        private void LoadDevices()
        {
            if (m_template == null)
                m_Devices = new List<InputDeviceVM>();
            else
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    m_Devices = new TableOperations<TemplateInputDevice>(connection)
                        .QueryRecordsWhere("TemplateID = {0}", m_template.Id)
                        .Select(d => new InputDeviceVM(d, m_template.Id)).ToList();

            m_Devices.ForEach(d => d.PropertyChanged += OnDeviceChange);
            OnPropertyChanged(nameof(Devices));
        }

        private void OnDeviceChange(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "Status")
                LoadDevices();
        }
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        
        #endregion
    }
}