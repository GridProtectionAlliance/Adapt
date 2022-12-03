// ******************************************************************************************************
// InputSignalViewModel.tsx - Gbtc
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
//  06/29/2021 - C. Lackner
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
using System.IO;
using System.Linq;
using System.Windows.Input;
using static Adapt.ViewModels.SelectSignalVM;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for <see cref="TemplateInputSignal"/>
    /// </summary>
    public class InputSignalVM : ViewModelBase, ISignalVM
    {
        #region [ Members ]

        private bool m_removed;
        private TemplateInputSignal m_model;
        private InputDeviceVM m_parent;
        private TemplateOutputSignal m_outputModel;

        private bool m_isOutput;
        private string m_outputName;
        private string m_name;
        private Phase m_phase;
        private MeasurementType m_type;

        #endregion

        #region[ Properties ]

        /// <summary>
        /// The Name of the <see cref="TemplateInputSignal"/>.
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
        /// Gets or Sets the <see cref="Phase"/> associated with this <see cref="TemplateInputSignal"/>
        /// </summary>
        public Phase Phase
        {
            get => m_phase;
            set
            {
                m_phase = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or Sets the <see cref="MeasurementType"/> associated with this <see cref="TemplateInputSignal"/>
        /// </summary>
        public MeasurementType Type
        {
            get => m_type;
            set
            {
                m_type = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag indicating if a Signal has been removed.
        /// </summary>
        public bool Removed => m_removed;

        /// <summary>
        /// <see cref="ICommand"/> associated with the remove Button if applicable.
        /// </summary>
        public ICommand Remove { get; }


        public bool IsInput => true;
        public int SectionOrder => -1;
        public int? ID => m_model?.ID;

        public bool IsOutput
        {
            get => m_isOutput;
            set {
                m_isOutput = value;
                OnPropertyChanged();
            }
        }

        public string OutputName
        {
            get => m_outputName;
            set {
                m_outputName = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Creates a new <see cref="TemplateInputSignal"/> ViewModel
        /// </summary>
        /// <param name="deviceViewModel"> The <see cref="TemplateInputDevice"/> ViewModel associated with this Device</param>
        /// <param name="signal">The <see cref="TemplateInputSignal"/> for this ViewModel </param>
        public InputSignalVM(InputDeviceVM deviceViewModel, TemplateInputSignal signal)
        {
            m_parent = deviceViewModel;
            m_model = signal;
            m_isOutput = false;
            m_outputModel = null;
            m_removed = false;

            if (!(signal is null))
            {
                m_name = signal.Name;
                m_phase = signal.Phase;
                m_type = signal.MeasurmentType;
                // Load the oUtput model
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    m_outputModel = new TableOperations<TemplateOutputSignal>(connection).QueryRecordWhere("SignalID = {0} AND IsInputSignal = 1", m_model.ID);
                m_isOutput = !(m_outputModel is null);
                m_outputName = m_outputModel?.Name ?? m_name;
            }

            Remove = new RelayCommand(Removal, () => m_parent.TemplateVM.Devices.Where(d => !d.Removed).Sum(d => d.NInputSignals) > 1);

        }

        /// <summary>
        /// Creates a new <see cref="TemplateInputSignal"/> ViewModel
        /// </summary>
        /// <param name="deviceViewModel"> The <see cref="TemplateInputDevice"/> ViewModel associated with this Device</param>
        /// <param name="signal">The <see cref="TemplateInputSignal"/> for this ViewModel </param>
        public InputSignalVM(InputDeviceVM deviceViewModel): this(deviceViewModel, null)
        {
            string name = "Signal 1";
            int i = 1;
            while (deviceViewModel.Signals.Where(d => d.Name == name && !d.Removed).Any() || deviceViewModel.AnalyticSignals.Where(d => d.Name == name && !d.Removed).Any())
            {
                i++;
                name = "Signal " + i.ToString();
            }

            m_name = name;
            m_outputName = name;
            m_phase = Phase.NONE;
            m_type = MeasurementType.Frequency;
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Type));
            OnPropertyChanged(nameof(Phase));
        }
        #endregion

        #region [ Methods ]

        public void Save()
        {
            if (m_removed)
                return;

            if (m_model is null)
            {
                m_model = new TemplateInputSignal()
                {
                    DeviceID = m_parent.ID ?? -1,
                    Name = m_name,
                    Phase = m_phase,
                    MeasurmentType = m_type       
                };

                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                {
                    new TableOperations<TemplateInputSignal>(connection).AddNewRecord(m_model);
                    m_model.ID = connection.ExecuteScalar<int>("select seq from sqlite_sequence where name = {0}", "TemplateInputSignal");
                }

            }
            else
            {

                m_model.Name = m_name;
                m_model.Phase = m_phase;
                m_model.MeasurmentType = m_type;
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<TemplateInputSignal>(connection).UpdateRecord(m_model);
            }


            // Output Models also need to be saved following the Signal Model
            if (IsOutput)
            {
                if (m_outputModel is null)
                {
                    m_outputModel = new TemplateOutputSignal()
                    {
                        IsInputSignal = true,
                        Name = m_outputName,
                        Phase = (int)m_phase,
                        SignalID = m_model.ID,
                        TemplateID = m_parent.TemplateVM.ID,
                        Type = (int)m_type,
                    };

                    using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    {
                        new TableOperations<TemplateOutputSignal>(connection).AddNewRecord(m_outputModel);
                        m_outputModel.ID = connection.ExecuteScalar<int>("select seq from sqlite_sequence where name = {0}", "TemplateOutputSignal");
                    }

                }
                else
                {

                    m_outputModel.Name = m_outputName;
                    m_outputModel.Phase = (int)m_phase;
                    m_outputModel.Type = (int)m_type;

                    using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                        new TableOperations<TemplateOutputSignal>(connection).UpdateRecord(m_outputModel);
                }
            }
        }

        // <summary>
        /// Delete any unused models form the Database.
        /// </summary>
        public void Delete()
        {
            if (m_model is null)
                return;

            if (m_removed || m_parent.Removed)
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<TemplateInputSignal>(connection).DeleteRecord(m_model.ID);

            if (!m_isOutput || m_removed || m_parent.Removed)
            {
                if (m_outputModel == null)
                    return;

                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<TemplateOutputSignal>(connection).DeleteRecord(m_outputModel.ID);
            }
        }

        public void Removal()
        {
            m_removed = true;
            OnPropertyChanged(nameof(Removed));
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
            changed = changed || m_model.Phase != m_phase;
            changed = changed || m_model.MeasurmentType != m_type;
            changed = changed || m_removed;
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
                issues.Add($"A Name is required for every Signal.");

            int nSig = m_parent.Signals.Where(item => item.Name == m_name).Count();
            if (nSig > 1)
                issues.Add($"A signal with name {m_name} already exists.");

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