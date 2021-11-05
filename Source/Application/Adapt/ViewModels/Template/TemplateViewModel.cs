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
using System.Threading;
using System.Transactions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for DataSource Window
    /// </summary>
    public class TemplateVM : AdaptTabViewModelBase, IViewModel
    {
        #region [ Members ]

        private Template m_template;
        private RelayCommand m_saveCommand;
        private RelayCommand m_deleteCommand;
        private RelayCommand m_clearCommand;
        private RelayCommand m_addDeviceCommand;
        private RelayCommand m_addSectionCommand;


        private ObservableCollection<InputDeviceVM> m_Devices;

        private bool m_changed;

        private ObservableCollection<SectionVM> m_Sections;

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
                m_changed = true;
                OnPropertyChanged(nameof(Changed));
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

        public bool Changed => m_changed || m_Devices.Where(d => d.Changed).Any() || m_Sections.Where(s => s.Changed).Any();
        
        public ICommand SaveCommand => m_saveCommand;

        public ICommand DeleteCommand => m_deleteCommand;

        public ICommand ClearCommand => m_clearCommand;

        /// <summary>
        /// Command that adds a <see cref="TemplateInputDevice"/> to this <see cref="Template"/>
        /// </summary>
        public ICommand AddDeviceCommand => m_addDeviceCommand;

        /// <summary>
        /// Command that adds a <see cref="SectionVM"/> to this <see cref="Template"/>
        /// </summary>
        public ICommand AddSectionCommand => m_addSectionCommand;

        public bool CanSave => Changed;

        public bool CanDelete => false;

        public bool CanClear => !Changed;
        

        public event CancelEventHandler BeforeLoad;
        public event EventHandler Loaded;
        public event CancelEventHandler BeforeSave;
        public event EventHandler Saved;
        public event CancelEventHandler BeforeDelete;
        public event EventHandler Deleted;

        public int NumberPMU => m_Devices?.Count() ?? 0;
        public int NumberSignals => m_Devices?.Sum(pmu => pmu.NSignals) ?? 0;

        public ObservableCollection<InputDeviceVM> Devices => m_Devices;

        public ObservableCollection<SectionVM> Sections => m_Sections;

        #endregion

        #region [ Constructor ]
        public TemplateVM(int ID): this() 
        {
            Load(ID);
        }

        public TemplateVM()
        {
            m_template = null;
            m_Devices = new ObservableCollection<InputDeviceVM>();

            m_clearCommand = new RelayCommand(Clear, () => CanClear);
            m_saveCommand = new RelayCommand(Save, () => CanSave);
            m_deleteCommand = new RelayCommand(Delete, () => CanDelete);
            m_addDeviceCommand = new RelayCommand(AddDevice, () => true);
            m_changed = false;
            m_Sections = new ObservableCollection<SectionVM>();
            m_addSectionCommand = new RelayCommand(AddSection, () => m_Sections.Count() < 10);
        }

        #endregion

        #region [ Methods ]
        /// <summary>
        /// Saves the entire Template and all associated Elements.
        /// </summary>
        public void Save()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                if (OnBeforeSaveCanceled())
                    throw new OperationCanceledException("Save was canceled.");

                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                {
                    TableOperations<Template> templateTbl = new TableOperations<Template>(connection);
                    templateTbl.AddNewOrUpdateRecord(m_template);
                }

                // Save Devices
                m_Devices.ToList().ForEach(d => d.Save());

                // Save Sections
                m_Sections.ToList().ForEach(s => s.Save());
             

                Load(m_template.Id);
                m_changed = false;
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

        private void AddDevice()
        {
            string name = "PMU 1";
            int i = 1;
            while (m_Devices.Where(d => d.Name == name).Any())
            {
                i++;
                name = "PMU " + i.ToString();
            }

            m_Devices.Add(new InputDeviceVM(this, new TemplateInputDevice()
            {
                Name = name,
                TemplateID = m_template?.Id ?? 0,
                ID = CreateDeviceID()
            }));
                
            m_Devices.Last().PropertyChanged += OnDeviceChange;
            OnPropertyChanged(nameof(Devices));
            OnPropertyChanged(nameof(Changed));
        }

        private void AddSection()
        {
            NewSectionView window = new NewSectionView();
            NewSectionVM vm = new NewSectionVM((TemplateSection section) => {
                section.TemplateID = m_template.Id;
                section.Order = m_Sections.Count() > 0 ? m_Sections.Max(s => s.Order) + 1 : 1;
                section.ID = CreateSectionID();
                m_Sections.Add(new SectionVM(section, this));
                m_Sections.Last().PropertyChanged += OnSectionChange;

                OnPropertyChanged(nameof(Sections));
                window.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(() =>
                {
                    window.Close();
                }));
            },this);
            window.DataContext = vm;
            
            window.Show();

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
                LoadSections();
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
                    Popup(ex.Message, "Load Template Exception:", MessageBoxImage.Error);
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
                m_Devices = new ObservableCollection<InputDeviceVM>();
            else
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    m_Devices = new ObservableCollection<InputDeviceVM>(new TableOperations<TemplateInputDevice>(connection)
                        .QueryRecordsWhere("TemplateID = {0}", m_template.Id)
                        .Select(d => new InputDeviceVM(this,d)));

            m_Devices.ToList().ForEach(d => d.PropertyChanged += OnDeviceChange);
            m_Devices.ToList().ForEach(d => d.LoadSignals());
            OnPropertyChanged(nameof(Devices));
        }

        private void OnDeviceChange(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "Changed")
                OnPropertyChanged(nameof(Changed));
        }

        private void OnSectionChange(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "Changed")
                OnPropertyChanged(nameof(Changed));
        }

        private void LoadSections()
        {
            if (m_template == null)
                m_Sections = new ObservableCollection<SectionVM>();
            else
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    m_Sections = new ObservableCollection<SectionVM>(new TableOperations<TemplateSection>(connection)
                        .QueryRecordsWhere("TemplateID = {0}", m_template.Id).OrderBy(item => item.Order)
                        .Select(d => new SectionVM(d, this)));

            m_Sections.ToList().ForEach(d => d.PropertyChanged += OnSectionChange);
            m_Sections.ToList().ForEach(d => d.LoadAnalytics());           

            OnPropertyChanged(nameof(Sections));
        }

        /// <summary>
        /// Creates a unique ID for a new Device. 
        /// All IDs < 0 to indicate they have never been saved to the Database.
        /// </summary>
        /// <returns> a negative unique deviceID</returns>
        public int CreateDeviceID()
        {
            if (m_Devices.Count() == 0)
                return -1;
            int min =  m_Devices.Min(item => item.ID);
            return (min < 0 ? (min - 1) : -1);
        }

        /// <summary>
        /// Creates a unique ID for a new InputSignal. 
        /// All IDs < 0 to indicate they have never been saved to the Database.
        /// </summary>
        /// <returns> a negative unique inpusSignalID</returns>
        public int CreateInputSignalID()
        {
            if (m_Devices.Count() == 0)
                return -1;
            int min = m_Devices.Min(item => item.NSignals > 0 ? item.Signals.Min(s => s.ID) : 0 );
            return (min < 0 ? (min - 1) : -1);
        }

        /// <summary>
        /// Creates a unique ID for a new Analytic. 
        /// All IDs < 0 to indicate they have never been saved to the Database.
        /// </summary>
        /// <returns> a negative unique analyticID</returns>
        public int CreateAnalyticID()
        {
            if (m_Sections.Count() == 0)
                return -1;
            int min = m_Sections.Min(item => item.Analytics.Count() > 0 ? item.Analytics.Min(a => a.ID) : 0);
            return (min < 0 ? (min - 1) : -1);
        }

        /// <summary>
        /// Creates a unique ID for a new Section. 
        /// All IDs < 0 to indicate they have never been saved to the Database.
        /// </summary>
        /// <returns> a negative unique sectionID</returns>
        public int CreateSectionID()
        {
            if (m_Sections.Count() == 0)
                return -1;
            int min = m_Sections.Min(item => item.ID);
            return (min < 0 ? (min - 1) : -1);
        }

        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        
        #endregion
    }
}