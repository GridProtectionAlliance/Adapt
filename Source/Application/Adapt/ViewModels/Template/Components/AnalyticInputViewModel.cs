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
    public class AnalyticInputVM : ViewModelBase
    {
        #region [ Members ]

        private bool m_removed;
        private AnalyticVM m_analytic;
        private AnalyticInput m_model;
        private ISignalVM m_signal;
        private TemplateVM m_template;

        private string m_label; // This is coming from the Analytic
        private int m_inputIndex;

        private bool m_isInputSignal;
        private int? m_SignalID;

        private SelectSignalWindow m_selectWindow;

        #endregion

        #region[ Properties ]

        /// <summary>
        /// The Label determined by the <see cref="IAnalytic"/>
        /// </summary>
        public string Label => m_label;

        public string Name => m_signal?.Name ?? "No Signal Selected";

        /// <summary>
        /// A Flag Indicating whether the Signal has been removed
        /// </summary>
        public bool Removed => m_removed;

        /// <summary>
        /// Flag indicating whether a <see cref="AnalyticInput"/> has been assigned.
        /// </summary>
        public bool Assigned { get; private set; }

        /// <summary>
        /// The <see cref="ICommand"/> associated with the Change Signal Button.
        /// </summary>
        public ICommand ChangeSignal { get; }
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Creates a new <see cref="AnalyticInput"/> ViewModel.
        /// </summary>
        /// <param name="analytic"> The <see cref="AnalyticVM"/> associated with this Input</param>
        /// <param name="analyticInputSignal">The <see cref="AnalyticOutputSignal"/> for this ViewModel </param>
        public AnalyticInputVM(AnalyticVM analytic, AnalyticInput analyticInputSignal)
        {

            m_removed = false;
            m_analytic = analytic;
            m_model = analyticInputSignal;

            m_signal = null;
            m_isInputSignal = true;

            m_template = analytic.TemplateVM;

            m_label = "";

            if (!(m_model is null))
            {
                m_inputIndex = m_model.InputIndex;
                m_isInputSignal = m_model.IsInputSignal;
                m_SignalID = m_model.SignalID;
            }

            if (!(m_SignalID is null))
            {
                if (m_isInputSignal)
                    m_signal = m_template.Devices.SelectMany(d => d.Signals).Where(s => s.ID == m_SignalID).FirstOrDefault();
                else
                    m_signal = m_template.Devices.SelectMany(d => d.AnalyticSignals).Where(s => s.ID == m_SignalID).FirstOrDefault();

                if (m_signal is null)
                    m_signal = m_template.Devices.SelectMany(d => d.Signals).Where(s => !s.Removed).FirstOrDefault();

                m_SignalID = m_signal?.ID;
                if (!(m_signal is null))
                    m_signal.PropertyChanged += SignalChanged;
                //m_device.PropertyChanged += DeviceChanged;
            }

            ChangeSignal = new RelayCommand(SelectSignal, () => true);
            m_analytic.SectionVM.PropertyChanged += SectionChanged;
        }

        /// <summary>
        /// Creates a new <see cref="AnalyticInput"/> ViewModel.
        /// </summary>
        /// <param name="analytic"> The <see cref="AnalyticVM"/> associated with this Input</param>
        public AnalyticInputVM(AnalyticVM analytic) : this(analytic, null)
        {

        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Opens the Dialog to select a new Signal for this input
        /// </summary>
        private void SelectSignal()
        {
            m_selectWindow = new SelectSignalWindow();
            SelectSignalVM viewModel = new SelectSignalVM(m_template, SelectSignalCVallback, (s) => s.IsInput || s.SectionOrder < m_analytic.SectionVM.Order);
            m_selectWindow.DataContext = viewModel;

            m_selectWindow.ShowDialog();
        }

        public void SelectSignalCVallback(ISignalVM signalVM)
        {
            m_signal = signalVM;
            m_SignalID = m_signal?.ID;
            m_isInputSignal = m_signal?.IsInput ?? true;

            if (!(m_signal is null))
                m_signal.PropertyChanged += SignalChanged;

            OnPropertyChanged(nameof(Name));
            m_selectWindow.Close();
        }
        /// <summary>
        /// Saves the <see cref="AnalyticInput"/> to the DataBase.
        /// </summary>
        public void Save()
        {

            if (m_removed)
                return;

            if (m_model is null)
            {

                m_model = new AnalyticInput()
                {
                    AnalyticID = m_analytic.ID ?? -1,
                    InputIndex = m_inputIndex,
                    IsInputSignal = m_isInputSignal,
                    SignalID = m_signal?.ID ?? -1
                };

                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                {
                    new TableOperations<AnalyticInput>(connection).AddNewRecord(m_model);
                    m_model.ID = connection.ExecuteScalar<int>("select seq from sqlite_sequence where name = {0}", "AnalyticInput");
                }

            }
            else
            {
                m_model.InputIndex = m_inputIndex;
                m_model.IsInputSignal = m_isInputSignal;
                m_model.SignalID = m_signal?.ID ?? -1;
 
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<AnalyticInput>(connection).UpdateRecord(m_model);
            }
        }

        /// <summary>
        /// Delete any unused models form the Database.
        /// </summary>
        public void Delete()
        {
            if (!m_removed && !m_analytic.Removed)
                return;

            if (m_model is null)
                return;

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                new TableOperations<AnalyticInput>(connection).DeleteRecord(m_model.ID);
        }

        public void AnalyticTypeUpdate(IAnalytic instance, int index)
        {
            if (instance.InputNames().Count() <= index)
            {
                m_removed = true;
                OnPropertyChanged(nameof(Removed));
                return;
            }
            m_inputIndex = index;
            m_removed = false;
            m_label = instance.InputNames().ElementAt(index);
            OnPropertyChanged(nameof(Removed));
            OnPropertyChanged(nameof(Label));
        }

        private void SectionChanged(object sender, PropertyChangedEventArgs arg)
        {
            if (arg.PropertyName == nameof(m_analytic.SectionVM.Order) && m_analytic.SectionVM.Order >= (m_signal?.SectionOrder ?? 0))
            {
                m_signal = null;
                m_SignalID = null;
                OnPropertyChanged(nameof(Name));
            }
        }

        private void SignalChanged(object sender, PropertyChangedEventArgs arg)
        {

            if (arg.PropertyName == nameof(m_signal.Name))
                OnPropertyChanged(nameof(Name));

            if (arg.PropertyName == nameof(m_signal.Removed) && m_signal.Removed)
            {
                m_signal.PropertyChanged -= SignalChanged;
                m_signal = null;
                m_SignalID = null;
                OnPropertyChanged(nameof(Name));
            }
        }

        /// <summary>
        /// Determines Whether the Device has Changed
        /// </summary>
        /// <returns></returns>
        public bool HasChanged()
        {
            if (m_model == null)
                return !m_removed;

            bool changed = (m_SignalID is null);
            changed = changed || (m_SignalID != m_model.SignalID) || m_isInputSignal != m_model.IsInputSignal;

            return changed;
        }

        /// <summary>
        /// Lists any Issues preventing this Analytic from being Saved
        /// </summary>
        /// <returns></returns>
        public List<string> Validate()
        {
            List<string> issues = new List<string>();

            if (m_removed)
                return issues;

            if (m_signal is null)
                issues.Add($"A valid Signal needs to be selected for {Label}");

            return issues;
        }

        /// <summary>
        /// Removes this <see cref="AnalyticInput"/>
        /// </summary>
        public void Removal()
        {
            m_removed = true;
            OnPropertyChanged(nameof(Removed));
        }

        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion
    }
}