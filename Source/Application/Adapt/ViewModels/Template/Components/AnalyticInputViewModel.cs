// ******************************************************************************************************
// AnalyticInputViewModel.tsx - Gbtc
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
//  08/13/2021 - C. Lackner
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
    /// ViewModel for an <see cref="AnalyticInput"/>
    /// </summary>
    public class AnalyticInputVM: ViewModelBase
    {
        #region [ Members ]

        private bool m_changed;
        private bool m_removed;
        private bool m_added;
        private AnalyticVM m_analyticVM;
        private AnalyticInput m_signal;
        private InputSignalVM m_inputSignalVM;
        private AnalyticOutputVM m_analyticOutputVM;

        private int m_inputIndex;
        #endregion

        #region[ Properties ]

        public string Label { get; }
        public string Name => m_inputSignalVM?.Name ?? m_analyticOutputVM?.Name ?? "";

        public int InputIndex => m_inputIndex;

        /// <summary>
        /// Flag indicating whether a <see cref="AnalyticInput"/> has been assigned.
        /// </summary>
        public bool Assigned { get; private set; }
        public bool Changed => m_changed;

        public bool Removed => m_removed;

        public bool Added => m_added;

        public InputSignalVM InputSignalViewModel => m_inputSignalVM;

        public AnalyticOutputVM AnalyticOutputViewModel => m_analyticOutputVM;

        /// <summary>
        /// The <see cref="ICommand"/> associated with the Change Signal Button.
        /// </summary>
        public ICommand ChangeSignal { get; }
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Creates a new <see cref="AnalyticInput"/> ViewModel.
        /// </summary>
        /// <param name="analyticViewModel"> The <see cref="AnalyticVM"/> associated with this Input</param>
        /// <param name="analyticInputSignal">The <see cref="AnalyticOutputSignal"/> for this ViewModel </param>
        /// <param name="label">The Label used for this Output based on the Analytic. </param>
        /// <param name="index"> The index of this Input for the specific Analytic.</param>
        public AnalyticInputVM(AnalyticVM analyticViewModel, AnalyticInput analyticInputSignal, string label, int index)
        {
            Label = label;
            m_changed = false;
            m_added = false;
            Assigned = true;
            m_inputIndex = index;
            m_removed = false;
            m_analyticVM = analyticViewModel;

            
            m_signal = analyticInputSignal;
            if (m_signal.IsInputSignal)
            {
                m_inputSignalVM = m_analyticVM.SectionViewModel.TemplateViewModel.Devices.Where(item => !item.Removed)
                    .SelectMany(item => item.Signals).Where(item => !item.Removed && item.ID == m_signal.SignalID).FirstOrDefault();
                m_inputSignalVM.PropertyChanged += InputSignalName_Change;
                m_analyticOutputVM = null;
            }
            else 
            {
                m_analyticOutputVM = m_analyticVM.Outputs.Where(item => !item.Removed && item.ID == m_signal.SignalID).FirstOrDefault();
                m_analyticOutputVM.PropertyChanged += InputSignalName_Change;
                m_inputSignalVM = null;
            }

            
            

            ChangeSignal = new RelayCommand(SelectSignal, () => true);

            analyticViewModel.SectionViewModel.TemplateViewModel.BeforeSave += ValidateBeforeSave;
        }

        #endregion

        #region [ Methods ]
        
        /// <summary>
        /// Opens the Dialog to select a new Signal for this input
        /// </summary>
        private void SelectSignal()
        {
            if (m_analyticVM.SectionViewModel.TemplateViewModel.Devices.Count() == 0)
            {
                Popup("There are no Devices Available. please add at least 1 Device", "No Devices Available", System.Windows.MessageBoxImage.Error);
                return;
            }
            if (m_analyticVM.SectionViewModel.TemplateViewModel.Devices.Sum(d => d.NSignals) == 0)
            {
                Popup("There are no Signals Available. please add at least 1 Signal", "No Signals Available", System.Windows.MessageBoxImage.Error);
                return;
            }

            SelectSignalWindow window = new SelectSignalWindow();
            SelectSignalVM viewModel = new SelectSignalVM(m_analyticVM.SectionViewModel.TemplateViewModel, UpdateSignal,m_analyticVM.SectionViewModel.Order);
            window.DataContext = viewModel;
            window.Show();
        }

        /// <summary>
        /// Updates the Selected Signal
        /// </summary>
        /// <param name="selectedSignal"> The new Signal selected. </param>
        private void UpdateSignal(AnalyticInput selectedSignal)
        {
            Assigned = true;
           
            selectedSignal.AnalyticID = m_analyticVM.ID;
            selectedSignal.InputIndex = m_inputIndex;

            m_signal = selectedSignal;
            
            m_changed = true;
            OnPropertyChanged(nameof(Changed));
            OnPropertyChanged(nameof(Assigned));

        }

        public void InputSignalName_Change(object sender, PropertyChangedEventArgs args) 
        {
            if (args.PropertyName == "Name") 
            {
                OnPropertyChanged(nameof(Name));
            }
            
        }

        /// <summary>
        /// Saves the <see cref="AnalyticInput"/> to the DataBase.
        /// </summary>
        public void Save()
        {
            if (!Changed && !Removed && !Added)
                return;

            if (m_signal.IsInputSignal) 
            {
                if (m_inputSignalVM.Added || m_inputSignalVM.Changed)
                    m_inputSignalVM.Save();
                m_signal.SignalID = m_inputSignalVM.SignalID;
            }

            if (!m_signal.IsInputSignal) 
            {
                if (m_analyticOutputVM.Added || m_analyticOutputVM.Changed)
                    m_analyticOutputVM.Save();
                m_signal.SignalID = m_analyticOutputVM.ID;
            }

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString)) 
            {
                TableOperations<AnalyticInput> tbl = new TableOperations<AnalyticInput>(connection);
                if (Removed)
                    tbl.DeleteRecord(m_signal);
                if (m_changed || m_added) 
                {
                    // TODO - What to do if m_added but not m_Changed
                    // ID has not been set.
                    tbl.AddNewOrUpdateRecord(m_signal);
                }
            }
        }

        private void ValidateBeforeSave(object sender, CancelEventArgs args)
        {
            if (m_analyticVM.Removed || m_analyticVM.SectionViewModel.Removed)
                return;


            if (!Assigned)
            {
                m_analyticVM.SectionViewModel.TemplateViewModel.AddSaveErrorMessage($"Analytic {m_analyticVM.Name} Input for {Label} needs to be assigned to a signal.");
                args.Cancel = true;
            }

            if (Assigned)
            {
                if (m_signal.IsInputSignal)
                {
                    InputSignalVM signalVM = m_analyticVM.SectionViewModel.TemplateViewModel.Devices.SelectMany(d => d.Signals).Where(s => s.ID == m_signal.SignalID).FirstOrDefault();
                    if (signalVM.Removed)
                    {
                        m_analyticVM.SectionViewModel.TemplateViewModel.AddSaveErrorMessage($"Analytic {m_analyticVM.Name} Input for {Label} needs to be assigned to a signal.");
                        args.Cancel = true;
                    }
                }
                else
                {
                    AnalyticOutputVM signalVM = m_analyticVM.SectionViewModel.TemplateViewModel.Sections
                        .SelectMany(s => s.Analytics).SelectMany(a => a.Outputs).Where(s => s.ID == m_signal.SignalID).FirstOrDefault();
                    if (signalVM.AnalyticVM.Removed)
                    {
                        m_analyticVM.SectionViewModel.TemplateViewModel.AddSaveErrorMessage($"Analytic {m_analyticVM.Name} Input for {Label} needs to be assigned to a signal.");
                        args.Cancel = true;
                    }
                }
            }
        }

        public void RemoveErrorMessages()
        {
            m_analyticVM.SectionViewModel.TemplateViewModel.BeforeSave -= ValidateBeforeSave;
        }

        // Remove this AnalyticInputViewModel but don't delete it from the database
        public void Remove() 
        {
            m_removed = true;
        }

        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        
        #endregion
    }
}