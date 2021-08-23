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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for <see cref="TemplateInputSignal"/>
    /// </summary>
    public class InputSignalVM: ViewModelBase
    {
        #region [ Members ]

        private bool m_removed;
        private bool m_selected;
        private bool m_changed;
        private TemplateInputSignal m_signal;
        private InputDeviceVM m_DeviceVM;
        #endregion

        #region[ Properties ]

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
            m_changed = false;
            m_signal = signal;
            m_DeviceVM = deviceViewModel;
            m_selected = false;
            OnSignalChange();
        }

        #endregion

        #region [ Methods ]

        public void Save()
        {
            if (!m_removed)
               using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                  new TableOperations<TemplateInputSignal>(connection).AddNewOrUpdateRecord(m_signal);
            else
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<TemplateInputSignal>(connection).DeleteRecord(m_signal);

        }

        public void Delete()
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

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion
    }
}