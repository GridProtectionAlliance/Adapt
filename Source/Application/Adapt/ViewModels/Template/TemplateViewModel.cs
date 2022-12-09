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
using Gemstone.Data;
using Gemstone.Data.Model;
using GemstoneWPF;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for DataSource Window
    /// </summary>
    public class TemplateVM : AdaptTabViewModelBase
    {
        #region [ Members ]

        private NewSectionView m_newSectionWindow;

        private bool m_removed;

        private Template m_model;

        private ObservableCollection<InputDeviceVM> m_Devices;

        private string m_name;

        private ObservableCollection<SectionVM> m_Sections;

        private List<string> m_saveErrors;

        #endregion

        #region[ Properties ]



        /// <summary>
        /// Indicates if the Section has been Removed.
        /// </summary>
        public bool Removed => m_removed;
        public string Name
        {
            get => m_name ?? m_model?.Name ?? "";
            set
            {
                m_name = value;
                OnPropertyChanged();
            }
        }
        public int ID => m_model?.Id ?? -1;

        public ICommand SaveCommand { get; }

        public ICommand DeleteCommand { get; }

        public ICommand ClearCommand { get; }

        /// <summary>
        /// Command that adds a <see cref="TemplateInputDevice"/> to this <see cref="Template"/>
        /// </summary>
        public ICommand AddDeviceCommand { get; }

        /// <summary>
        /// Command that adds a <see cref="SectionVM"/> to this <see cref="Template"/>
        /// </summary>
        public ICommand AddSectionCommand { get;  }       

        public event CancelEventHandler BeforeLoad;
        public event EventHandler Loaded;
        public event CancelEventHandler BeforeSave;
        public event EventHandler Saved;
        public event CancelEventHandler BeforeDelete;
        public event EventHandler Deleted;

        public int NumberPMU => m_Devices?.Count() ?? 0;
        public int NumberSignals => m_Devices?.Sum(pmu => pmu.NInputSignals) ?? 0;

        public ObservableCollection<InputDeviceVM> Devices => m_Devices;

        public ObservableCollection<TemplateOutputDeviceWrapper> OutputDevices => new ObservableCollection<TemplateOutputDeviceWrapper>(m_Devices.Select(d => new TemplateOutputDeviceWrapper(d)));
        public ObservableCollection<SectionVM> Sections => m_Sections;

        #endregion

        #region [ Constructor ]
        public TemplateVM(int ID): this() 
        {
            m_removed = false;
            m_saveErrors = new List<string>();
            Load(ID);
        }

        public TemplateVM()
        {
            m_model = null;
            m_name = "";

            m_Devices = new ObservableCollection<InputDeviceVM>();

            ClearCommand = new RelayCommand(Clear, HasChanged);
            SaveCommand = new RelayCommand(Save, () => HasChanged() && isValid());
            DeleteCommand = new RelayCommand(Delete, () => true);
            AddDeviceCommand = new RelayCommand(AddDevice, () => true);
            
            m_Sections = new ObservableCollection<SectionVM>();
            AddSectionCommand = new RelayCommand(AddSection, () => m_Sections.Count() < 10);
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
                if (m_model is null)
                    throw new OperationCanceledException("No Template was loaded.");

                if (OnBeforeSaveCanceled())
                    throw new OperationCanceledException("Save was canceled.");

                // update Template
                m_model.Name = m_name;
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<Template>(connection).UpdateRecord(m_model);

                // update or Add Devices
                foreach (InputDeviceVM d in Devices)
                    d.Save();

                // update Sections
                foreach (SectionVM s in Sections)
                    s.Save();

                // Remove Extra Sections in Reverse Order
                foreach (SectionVM s in Sections.Reverse())
                    s.Delete();

                //remove Extra Devices
                foreach (InputDeviceVM d in Devices)
                    d.Delete();
            }
            catch (Exception ex)
            {

                Popup("An error occurred while saving this template. Please see the Logs or contact GPA.", "Error", MessageBoxImage.Error);

                if (ex.InnerException != null)
                    Log.Logger.Error(ex, $"Template {ID} Save Failed Exception: {ex.InnerException.Message} StackTrace: {ex.InnerException.StackTrace}");
                else
                    Log.Logger.Error(ex, $"Template {ID} Save Failed Exception: {ex.Message} StackTrace: {ex.StackTrace}");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Creates a new Device
        /// </summary>
        /// <param name="isInput"> indicates if that device is supposed to be available as an input</param>
        /// <returns>The <see cref="InputDeviceVM"/> for the Device in question</returns>
        public void AddDevice()
        {
            InputDeviceVM d = new InputDeviceVM(this);
            d.PropertyChanged += (object src, PropertyChangedEventArgs arg) => { if (arg.PropertyName == nameof(d.Removed)) OnPropertyChanged(nameof(Devices)); };
            m_Devices.Add(d);
            OnPropertyChanged(nameof(Devices));
            OnPropertyChanged(nameof(OutputDevices));
        }

        private void AddSection()
        {

            m_newSectionWindow = new NewSectionView();
            SectionVM vm = new SectionVM(this);
            m_newSectionWindow.DataContext = vm;

            m_newSectionWindow.ShowDialog();


        }

        /// <summary>
        /// Callback triggered by the Section added to the Template
        /// </summary>
        /// <param name="section"></param>
        public void AddSectionCallback(SectionVM section)
        {
            m_newSectionWindow.Close();
            m_Sections.Add(section);
            OnPropertyChanged(nameof(Sections));
        }

        public void Delete() 
        {
            m_removed = true;

            foreach (SectionVM s in Sections)
                s.Removal();
            foreach (InputDeviceVM d in Devices)
                d.Removal();

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                // Remove Extra Sections in Reverse Order
                foreach (SectionVM s in Sections.Reverse())
                    s.Delete();

                //remove Extra Devices
                foreach (InputDeviceVM d in Devices)
                    d.Delete();

                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<Template>(connection).DeleteRecord(m_model.Id);


                OnDeleted();
            }
            catch (Exception ex)
            {
                Popup("An error occurred while deleting this template. Please see the Logs or contact GPA.", "Error",MessageBoxImage.Error);

                if (ex.InnerException != null)
                    Log.Logger.Error(ex, $"Template {ID} Delete Failed Exception: {ex.InnerException.Message} StackTrace: {ex.InnerException.StackTrace}");
                else
                    Log.Logger.Error(ex, $"Template {ID} Delete Failed Exception: {ex.Message} StackTrace: {ex.StackTrace}");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

        }

        /// <summary>
        /// Raises the <see cref="BeforeDelete"/> event.
        /// </summary>
        private bool OnBeforeDeleteCanceled()
        {
            CancelEventArgs cancelEventArgs = new CancelEventArgs();

            if (BeforeDelete != null)
                BeforeDelete(this, cancelEventArgs);

            return cancelEventArgs.Cancel;
        }

        /// <summary>
        /// Raises the <see cref="Deleted"/> event.
        /// </summary>
        private void OnDeleted()
        {
            if (Deleted != null)
                Deleted(this, EventArgs.Empty);
        }

        public void Clear()
        {
            if (!(m_model is null))
                Load();
        }

        public void Load() => Load(m_model?.Id ?? 1);
        
        public void Load(int ID)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                if (OnBeforeLoadCanceled())
                    throw new OperationCanceledException("Load was canceled.");

                // Remove all old subscriptions to onSave event to make sure all old VMs get disposed and can't raise errors
                BeforeSave = null;

                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    m_model = new TableOperations<Template>(connection).QueryRecordWhere("Id = {0}", ID);

                if (!(m_model is null))
                    m_name = m_model.Name;

                LoadDevices();
                LoadSections();
                OnLoaded();

                // Ensure at least 1 Input Signal and Device exist
                if (m_Devices.Count() == 0)
                    AddDevice();

                OnPropertyChanged(nameof(Name));
            }
            catch(Exception ex)
            {
                Popup( "An error occurred while loading this template. Please see the Logs or contact GPA.", "Error", MessageBoxImage.Error);

                if (ex.InnerException != null)
                    Log.Logger.Error(ex, $"Template {ID} Load Failed Exception: {ex.InnerException.Message} StackTrace: {ex.InnerException.StackTrace}");
                else
                    Log.Logger.Error(ex, $"Template {ID} Load Failed Exception: {ex.Message} StackTrace: {ex.StackTrace}");
                
            }
            finally
            {
                m_removed = false;
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

            m_saveErrors = new List<string>();

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

        private void LoadDevices()
        {
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                m_Devices = new ObservableCollection<InputDeviceVM>(new TableOperations<TemplateInputDevice>(connection)
                    .QueryRecordsWhere("TemplateID = {0}", m_model.Id)
                    .Select(d => {
                        InputDeviceVM r = new InputDeviceVM(this, d);
                        r.PropertyChanged += (object src, PropertyChangedEventArgs arg) => { if (arg.PropertyName == nameof(r.Removed)) OnPropertyChanged(nameof(Devices)); };
                        return r;
                    }));

            OnPropertyChanged(nameof(Devices));
            OnPropertyChanged(nameof(OutputDevices));
        }

        private void LoadSections()
        {
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                m_Sections = new ObservableCollection<SectionVM>(new TableOperations<TemplateSection>(connection)
                    .QueryRecordsWhere("TemplateID = {0}", m_model.Id).OrderBy(item => item.Order)
                    .Select(s => new SectionVM(s, this)));

            OnPropertyChanged(nameof(Sections));

        }


        public bool HasChanged()
        {
            if (m_model == null)
                return false;

            bool changed = m_model.Name != m_name;
            changed = changed || m_Devices.Any(d => d.HasChanged());
            changed = changed || m_Sections.Any(d => d.HasChanged());
            return changed;
        }

        public List<string> Validate()
        {
            List<string> issues = new List<string>();

            if (m_model is null)
                return new List<string>() { "No template was loaded." };

            if (string.IsNullOrEmpty(m_name))
                issues.Add($"{m_name} is not a valid name.");

            if (OutputDevices.Count(o => o.Enabled ?? true) == 0)
                issues.Add($"At least one output is required.");

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                if (new TableOperations<Template>(connection).QueryRecordCountWhere("Name = {0} AND ID <> {1}",m_name, m_model.Id) > 1)
                    issues.Add($"{m_name} is not a valid name.");

            issues.AddRange(m_Devices.SelectMany(d => d.Validate()));
            issues.AddRange(m_Sections.SelectMany(d => d.Validate()));
            
            return issues;
        }

        /// <summary>
        /// Swap the order of two Sections. 
        /// </summary>
        /// <param name="index1"> The index of the first section</param>
        /// <param name="index2">The index of the second section</param>
        public void SwapSectionOrder(int index1, int index2)
        {
            if (index1 == index2)
                return;

            if (index1 >= m_Sections.Count() || index1 < 0)
                return;
            if (index2 >= m_Sections.Count() || index2 < 0)
                return;

            int order1 = m_Sections[index1].Order;
            int order2 = m_Sections[index2].Order;

            m_Sections[index1].Order = order2;
            m_Sections[index2].Order = order1;

            if (index1 < index2)
            {
                m_Sections.Move(index1, index2);
                m_Sections.Move(index2-1, index1);
            }
            else
            {
                m_Sections.Move(index2, index1);
                m_Sections.Move(index1 - 1, index2);
            }

        }
        public bool isValid() => Validate().Count == 0;

        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        
        #endregion
    }
}