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
        private AnalyticVM m_analyticVM;
        private AnalyticInput m_signal;
        private string m_name;
        private int m_inputIndex;
        #endregion

        #region[ Properties ]

        public string Label { get; }
        public string Name { 
            get => m_name;
            set
            {
                m_name = value;
                OnPropertyChanged();
                     
            }
        }

        /// <summary>
        /// Flag indicating whether a <see cref="AnalyticInput"/> has been assigned.
        /// </summary>
        public bool Assigned { get; private set; }
        public bool Changed => m_changed;
       
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
            Assigned = true;
            m_inputIndex = index;
            m_analyticVM = analyticViewModel;

            if (analyticInputSignal == null || analyticInputSignal.SignalID == 0)
            {
                m_name = "Not Assigned";
                Assigned = false;
            }
            else
                GetSignalName(analyticInputSignal);

            m_signal = analyticInputSignal;

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

            GetSignalName(selectedSignal);
            m_signal = selectedSignal;
            
            m_changed = true;
            OnPropertyChanged(nameof(Changed));
            OnPropertyChanged(nameof(Assigned));

        }

        /// <summary>
        /// Update Name Property with correct SignalName
        /// </summary>
        /// <param name="signal">The <see cref="AnalyticInput"/> with the correct signalID</param>
        private void GetSignalName(AnalyticInput signal)
        {
            if (m_signal != null)
            {
                if (m_signal.IsInputSignal)
                {
                    InputSignalVM signalVM = m_analyticVM.SectionViewModel.TemplateViewModel.Devices.SelectMany(d => d.Signals).Where(s => s.ID == m_signal.SignalID).FirstOrDefault();
                    signalVM.PropertyChanged -= SignalNameChanged;
                }
                else
                {
                    AnalyticOutputVM signalVM = m_analyticVM.SectionViewModel.TemplateViewModel.Sections
                        .SelectMany(s => s.Analytics).SelectMany(a => a.Outputs).Where(s => s.ID == m_signal.SignalID).FirstOrDefault();
                    if (signalVM != null)
                        signalVM.PropertyChanged -= SignalNameChanged;
                }
            }

            if (signal.IsInputSignal)
            {
                InputSignalVM signalVM = m_analyticVM.SectionViewModel.TemplateViewModel.Devices.SelectMany(d => d.Signals).Where(s => s.ID == signal.SignalID).FirstOrDefault();
                m_name = signalVM?.Name ?? "";
                signalVM.PropertyChanged += SignalNameChanged;
            }
            else
            {
                AnalyticOutputVM signalVM = m_analyticVM.SectionViewModel.TemplateViewModel.Sections
                    .SelectMany(s => s.Analytics).SelectMany(a => a.Outputs).Where(s => s.ID == signal.SignalID).FirstOrDefault();
                if (signalVM == null)

                    m_name = "";
                else
                {
                    signalVM.PropertyChanged += SignalNameChanged;
                    m_name = signalVM?.Name ?? "";
                }
            }

            OnPropertyChanged(nameof(Name));
        }
        /// <summary>
        /// Function to update Name if the Signal Name changes.
        /// </summary>
        private void SignalNameChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "Name" && m_signal != null)
            {
                if (m_signal.IsInputSignal)
                {
                    InputSignalVM signalVM = (InputSignalVM)sender;
                    m_name = signalVM?.Name ?? "";
                }
                else
                {
                    AnalyticOutputVM signalVM = (AnalyticOutputVM)sender;
                    m_name = signalVM?.Name ?? "";
                }
                OnPropertyChanged(nameof(Name));
            }
            
        }

        /// <summary>
        /// Saves the <see cref="AnalyticInput"/> to the DataBase.
        /// </summary>
        public void Save()
        {
            if (!Changed)
                return;

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            {
                int templateId = new TableOperations<Template>(connection).QueryRecordWhere("Name = {0}", m_analyticVM.SectionViewModel.TemplateViewModel.Name).Id;
                int sectionId = new TableOperations<TemplateSection>(connection).QueryRecordWhere("[Order] = {0} AND TemplateId = {1}", m_analyticVM.SectionViewModel.Order, templateId).ID;
                int analyticID = new TableOperations<Analytic>(connection).QueryRecordWhere("Name = {0} AND TemplateID = {1} AND SectionID = {2}", m_analyticVM.Name, templateId, sectionId).ID;
                int signalID = 0;
                if (m_signal.IsInputSignal)
                {
                    InputSignalVM signalVM = m_analyticVM.SectionViewModel.TemplateViewModel.Devices.SelectMany(d => d.Signals)
                        .Where(s => s.ID == m_signal.SignalID).FirstOrDefault();
                    int deviceId = new TableOperations<TemplateInputDevice>(connection).QueryRecordWhere("Name = {0} AND TemplateID = {1}",
                        m_analyticVM.SectionViewModel.TemplateViewModel.Devices.FirstOrDefault(item => item.ID == signalVM.DeviceID).Name, templateId).ID;
                    signalID = new TableOperations<TemplateInputSignal>(connection).QueryRecordWhere("Name = {0} AND DeviceID = {1}", signalVM.Name, deviceId).ID;
                }
                else
                {
                    AnalyticOutputVM signalVM = m_analyticVM.SectionViewModel.TemplateViewModel.Sections
                        .SelectMany(s => s.Analytics).SelectMany(a => a.Outputs).Where(s => s.ID == m_signal.SignalID).FirstOrDefault();
                    signalID = new TableOperations<AnalyticOutputSignal>(connection).QueryRecordWhere("AnalyticID = {0} AND OutputIndex = {1}", analyticID, signalVM.OutputIndex).ID;
                }

                TableOperations<AnalyticInput> tbl = new TableOperations<AnalyticInput>(connection);
                tbl.DeleteRecordWhere("AnalyticID = {0} AND InputIndex = {1} AND ID <> {2}", analyticID,m_signal.InputIndex,m_signal.ID);

                tbl.AddNewRecord(m_signal);  
            }
        }

        private void ValidateBeforeSave(object sender, CancelEventArgs args)
        {
            if (!Assigned)
            {
                m_analyticVM.SectionViewModel.TemplateViewModel.AddSaveErrorMessage($"Analytic {m_analyticVM.Name} Input for {Label} needs to be assigned to a signal.");
                args.Cancel = true;
            }
        }

        public void RemoveErrorMessages()
        {
            m_analyticVM.SectionViewModel.TemplateViewModel.BeforeSave -= ValidateBeforeSave;
        }

        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion
    }
}