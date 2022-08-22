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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for <see cref="TemplateInputSignal"/>
    /// </summary>
    public class InputSignalVM: ViewModelBase
    {
        #region [ Members ]

        private bool m_removed;
        private bool m_changed;
        private bool m_added;
        private bool m_noModel;
        private TemplateInputSignal m_signal;
        private InputDeviceVM m_DeviceVM;
        #endregion

        #region[ Properties ]

        public bool Added => m_added;

        /// <summary>
        /// The Name of the <see cref="TemplateInputSignal"/>.
        /// </summary>
        public string Name
        {
            get => m_signal.Name;
            set
            {
                m_signal.Name = value;
                OnPropertyChanged();
                m_changed = true;
                OnPropertyChanged(nameof(Changed));
            }
        }

        /// <summary>
        /// Gets or Sets the <see cref="Phase"/> associated with this <see cref="TemplateInputSignal"/>
        /// </summary>
        public Phase Phase
        {
            get => m_signal.Phase;
            set
            {
                m_signal.Phase = value;
                OnPropertyChanged();
                m_changed = true;
                OnPropertyChanged(nameof(Changed));
            }
        }

        /// <summary>
        /// Gets or Sets the <see cref="MeasurementType"/> associated with this <see cref="TemplateInputSignal"/>
        /// </summary>
        public MeasurementType Type
        {
            get => m_signal.MeasurmentType;
            set
            {
                m_signal.MeasurmentType = value;
                OnPropertyChanged();
                m_changed = true;
                OnPropertyChanged(nameof(Changed));
            }
        }

        /// <summary>
        /// <see cref="ICommand"/> associated with the remove Button if applicable.
        /// </summary>
        public ICommand Remove { get; }

        /// <summary>
        /// Flag indicating if this <see cref="TemplateInputSignal"/> has changed.
        /// This is also set if the Signal is removed
        /// </summary>
        public bool Changed => m_changed || m_removed;

        /// <summary>
        /// Flag  indicating if the Input Signal has been removed
        /// </summary>
        public bool Removed => m_removed;

        /// <summary>
        /// The unique ID of the <see cref="TemplateInputSignal"/>
        /// </summary>
        public int ID => m_signal.ID;

        /// <summary>
        /// The ID saved to the DataBase.
        /// </summary>
        public int SignalID { get; private set;}

        /// <summary>
        /// The ID of the Device this signal is associated with.
        /// </summary>
        public int DeviceID => m_signal.DeviceID;
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Creates a new <see cref="TemplateInputSignal"/> VieModel
        /// </summary>
        /// <param name="deviceViewModel"> The <see cref="TemplateInputDevice"/> ViewModel associated with this Device</param>
        /// <param name="signal">The <see cref="TemplateInputSignal"/> for this ViewModel </param>
        public InputSignalVM(InputDeviceVM deviceViewModel, TemplateInputSignal signal)
        {
            m_removed = false;
            m_added = signal.ID < 0;
            m_changed = false;
            m_noModel = false;
            m_signal = signal;
            m_DeviceVM = deviceViewModel;
            Remove = new RelayCommand(Rmv, () => true);
            SignalID = m_signal.ID;
            OnSignalChange();
        }

        /// <summary>
        /// Creates a new <see cref="InputSignalVM"> with no associated model
        /// </summary>
        /// <param name="deviceViewModel"></param>
        public InputSignalVM(InputDeviceVM deviceViewModel) 
        {
            // m_signal and SignalID will remain undefined when the viewmodel is created
            m_removed = false;
            m_changed = false;
            m_added = false;
            m_noModel = true;
            m_DeviceVM = deviceViewModel;
            Remove = new RelayCommand(Rmv, () => true);
            OnSignalChange();
        }

        #endregion

        #region [ Methods ]

        public void Save()
        {
            /*
            if (!m_removed)
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                {

                    int templateId = new TableOperations<Template>(connection).QueryRecordWhere("Name = {0}", m_DeviceVM.TemplateViewModel.Name).Id;
                    int deviceID = new TableOperations<TemplateInputDevice>(connection).QueryRecordWhere("Name = {0} AND templateID = {1}", m_DeviceVM.Name, templateId).ID;
                    int sectionID = new TableOperations<TemplateSection>(connection).QueryRecordWhere("templateID = {0}", templateId).ID;
                    int analyticID = new TableOperations<Analytic>(connection).QueryRecordWhere("sectionID = {0}", sectionID).ID;

                    m_signal.DeviceID = deviceID;

                    if (m_signal.ID < 0)
                        new TableOperations<TemplateInputSignal>(connection).AddNewRecord(m_signal);
                    else
                        new TableOperations<TemplateInputSignal>(connection).AddNewOrUpdateRecord(m_signal);

                    SignalID = new TableOperations<TemplateInputSignal>(connection).QueryRecordWhere("DeviceID = {0} AND Name = {1}", m_signal.DeviceID, m_signal.Name).ID;
                }
            
            else
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<TemplateInputSignal>(connection).DeleteRecord(m_signal);
            */
            if (!m_changed && !m_added && !m_removed)
                return;

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString)) 
            {
                TableOperations<TemplateInputSignal> tbl = new TableOperations<TemplateInputSignal>(connection);
                if (m_removed)
                    tbl.DeleteRecord(m_signal);
                if (!m_removed && m_added) 
                    tbl.AddNewRecord(m_signal);
                if (!m_removed && m_changed)
                    tbl.UpdateRecord(m_signal);
                
            }
        }

        // Delete this signal from the database
        public void Delete()
        {
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                new TableOperations<TemplateInputSignal>(connection).DeleteRecord(m_signal);
        }

        // Remove this signal but don't delete it from the database
        public void Rmv() 
        {
            m_removed = true;
            OnPropertyChanged(nameof(Changed));
            OnPropertyChanged(nameof(Removed));
        }

        private void OnSignalChange()
        {
            OnPropertyChanged(nameof(Name));

        }

        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion
    }
}