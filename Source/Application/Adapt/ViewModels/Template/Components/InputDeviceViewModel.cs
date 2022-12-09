// ******************************************************************************************************
//  DeviceViewModel.tsx - Gbtc
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
//  04/01/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using Adapt.Models;
using Gemstone.Data;
using Gemstone.Data.Model;
using Gemstone.IO;
using GemstoneCommon;
using GemstoneWPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for a <see cref="TemplateInputDevice"/>
    /// </summary>
    public class InputDeviceVM: ViewModelBase
    {
        #region [ Members ]

        private TemplateInputDevice m_model;

        private bool m_removed;

        private bool m_isInput;
        private string m_name;
        private string m_outputName;


        private TemplateVM m_parentVM;

        #endregion

        #region[ Properties ]

        public bool Removed => m_removed;

        /// <summary>
        /// The Name of the Device.
        /// </summary>
        public string Name
        {
            get => m_name ?? m_model?.Name ?? "";
            set
            {
                m_name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The OutputName of the Device.
        /// </summary>
        public string OutputName
        {
            get => m_outputName ?? m_model?.OutputName ?? "";
            set
            {
                m_outputName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The number of Input Signals associated with this Device
        /// </summary>
        public int NInputSignals => Signals.Count(i => !i.Removed);

      
        /// <summary>
        /// The number of Analytic Output Signals associated with this Device
        /// </summary>
        public int NAnalyticSignals => AnalyticSignals.Count(i => !i.Removed);

        /// <summary>
        /// A Flag indicating if this device is visible in the Input List
        /// </summary>
        public bool VisibleInput => !m_removed;

        /// <summary>
        /// ViewModels for all <see cref="TemplateInputSignal"/> associated with this <see cref="TemplateInputDevice"/>
        /// </summary>
        public ObservableCollection<InputSignalVM> Signals { get; private set; }

        /// <summary>
        /// ViewModels for all <see cref="AnalyticOutputVM"/> associated with this <see cref="TemplateInputDevice"/>
        /// </summary>
        public ObservableCollection<AnalyticOutputVM> AnalyticSignals { get; private set; }

        /// <summary>
        /// <see cref="ICommand"/> associated with the remove Button if applicable.
        /// </summary>
        public ICommand Remove { get; }


        /// <summary>
        /// <see cref="ICommand"/> to add a new Signal to this Device.
        /// </summary>
        public ICommand AddSignal { get; }

        /// <summary>
        /// The ID of the associated Model.
        /// Note that this will be <see cref="Nullable"/> and is NULL if it's a new Model
        /// </summary>
        public int? ID => m_model?.ID ?? null;

        public TemplateVM TemplateVM => m_parentVM;

        /// <summary>
        /// Required for WPF Property Change Notifications. This comes from <see cref="ViewModelBase"/>
        /// </summary>
        public event PropertyChangedEventHandler SignalPropertyChanged;

        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Creates a new ViewModel for a <see cref="TemplateInputDevice"/>
        /// </summary>
        /// <param name="templateViewModel"> The View model for this <see cref="Template"/> </param>
        /// <param name="device"> The <see cref="TemplateInputDevice"/> associated with this ViewModel</param>
        public InputDeviceVM(TemplateVM templateViewModel, TemplateInputDevice device)
        {
            m_model = device;
            Signals = new ObservableCollection<InputSignalVM>();
            AnalyticSignals = new ObservableCollection<AnalyticOutputVM>();
            m_removed = false;
            Remove = new RelayCommand(Removal, () => m_parentVM.Devices.Where(d => !d.Removed).Count() > 1);
            AddSignal = new RelayCommand(AddNewSignal, () => true);
            m_parentVM = templateViewModel;

            LoadSignals();

            if (templateViewModel.Devices.Sum(d => d.NInputSignals) == 0)
                AddNewSignal();

            m_isInput = m_model?.IsInput ?? true;
            m_name = m_model?.Name ?? "PMU 1";
            m_outputName = m_model?.OutputName ?? m_name;
        }
        public InputDeviceVM(TemplateVM templateViewModel): this(templateViewModel, null)
        {
            string name = "PMU 1";
            int i = 1;
            while (templateViewModel.Devices.Where(d => !d.Removed && d.Name == name).Any())
            {
                i++;
                name = "PMU " + i.ToString();
            }

            m_name = name;
            m_outputName = name;

            if (templateViewModel.Devices.Sum(d => d.NInputSignals) == 0)
                AddNewSignal();

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(OutputName));

        }

        #endregion

        #region [ Methods ]

        public void Save()
        {

            if (m_removed)
                return;

            if (m_model is null)
            {
                m_model = new TemplateInputDevice()
                {
                    IsInput = m_isInput,
                    Name = m_name,
                    OutputName = m_outputName,
                    TemplateID = m_parentVM.ID
                };

                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                {
                    new TableOperations<TemplateInputDevice>(connection).AddNewRecord(m_model);
                    m_model.ID = connection.ExecuteScalar<int>("select seq from sqlite_sequence where name = {0}", "TemplateInputDevice");
                }

            }
            else
            {
                //IsInput is depreciated. It is still in the Database, but not used anywhere
                m_model.IsInput = true;
                m_model.Name = m_name;
                m_model.OutputName = m_outputName;
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<TemplateInputDevice>(connection).UpdateRecord(m_model);
            }

            foreach (InputSignalVM signal in Signals)
                signal.Save();
        }

        // <summary>
        /// Delete any unused models form the Database.
        /// </summary>
        public void Delete()
        {

            foreach (InputSignalVM signal in Signals)
                signal.Delete();

            if (!m_removed && !m_parentVM.Removed)
                return;

            if (m_model is null)
                return;

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                new TableOperations<TemplateInputDevice>(connection).DeleteRecord(m_model.ID);
        }

        public void Removal()
        {
            m_removed = true;
            foreach (InputSignalVM s in Signals)
                s.Removal();

            OnPropertyChanged(nameof(Removed));
        }

        public void RegisterAnalyticOutput(AnalyticOutputVM output)
        {
            output.PropertyChanged += SignalChanged;
            AnalyticSignals.Add(output);
            OnPropertyChanged(nameof(AnalyticSignals));
            OnPropertyChanged(nameof(NAnalyticSignals));
        }

        public void DeRegisterAnalyticOutput(AnalyticOutputVM output)
        {
            output.PropertyChanged -= SignalChanged;
            AnalyticSignals.Remove(output);
            OnPropertyChanged(nameof(AnalyticSignals));
            OnPropertyChanged(nameof(NAnalyticSignals));
        }

        private void SignalChanged (object sender, PropertyChangedEventArgs args) 
        {
            if (SignalPropertyChanged != null)
                SignalPropertyChanged.Invoke(sender, args);
        }

        public void LoadSignals() 
        {
            if (m_model is null)
                return;

            List<TemplateInputSignal> signals;
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                signals =new TableOperations<TemplateInputSignal>(connection).QueryRecordsWhere("DeviceId = {0}", m_model.ID).ToList();

            foreach (InputSignalVM vm in signals.Select(item => new InputSignalVM(this,item)))
            {
                vm.PropertyChanged += SignalChanged;
                Signals.Add(vm);
            }
        }

        private void AddNewSignal()
        {

            InputSignalVM signal = new InputSignalVM(this);
            signal.PropertyChanged += SignalChanged;

            Signals.Add(signal);
            
            OnPropertyChanged(nameof(Signals));
            OnPropertyChanged(nameof(NInputSignals));
        }

        /// <summary>
        /// Determines Whether the Device has Changed
        /// </summary>
        /// <returns></returns>
        public bool HasChanged()
        {
            if (m_model == null)
                return !m_removed;
            
            bool changed = m_model.Name != m_name;
            changed = changed || m_model.IsInput != m_isInput;
            changed = changed || m_model.OutputName != m_outputName;

            changed = changed || Signals.Any(s => s.HasChanged());
            changed = changed || m_removed;

            int nOutSig = Signals.Where(s => s.IsOutput && !s.Removed).Count() + AnalyticSignals.Where(s => s.IsOutput && !s.Removed).Count();
            changed = changed || (m_outputName != m_model.OutputName && nOutSig > 0);
            return changed;
           
        }

        /// <summary>
        /// Lists any Issues preventing thisDevice from being Saved
        /// </summary>
        /// <returns></returns>
        public List<string> Validate()
        {
            List<string> issues = new List<string>();

            if (m_removed)
                return issues;

            if (string.IsNullOrEmpty(m_name))
                issues.Add($"A name is required for every PMU.");

            int nDev = m_parentVM.Devices.Where(item => item.Name == m_name).Count();
            if (nDev > 1)
                issues.Add($"A PMU with name {m_name} already exists.");

            if (string.IsNullOrEmpty(m_outputName))
                issues.Add($"{m_outputName} is not a valid output name.");

            issues.AddRange(Signals.SelectMany(d => d.Validate()));

            return issues;
        }

        /// <summary>
        /// Indicates whether the Device can be saved.
        /// </summary>
        /// <returns> <see cref="true"/> If the device can be saved</returns>
        public bool isValid() => Validate().Count() == 0;
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion
    }
}