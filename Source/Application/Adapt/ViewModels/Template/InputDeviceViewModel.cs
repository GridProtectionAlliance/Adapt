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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for Template Input Device
    /// </summary>
    public class InputDeviceVM: ViewModelBase
    {
        #region [ Members ]

        private TemplateInputDevice m_device;
        private ObservableCollection<InputSignalVM> m_signals;
        private RelayCommand m_removeCmd;
        private RelayCommand m_addSingalCmd;
        private bool m_removed;
        private bool m_changed;
        #endregion

        #region[ Properties ]

        public string Name
        {
            get => m_device.Name;
            set
            {
                m_device.Name = value;
                m_changed = true;
                OnPropertyChanged(nameof(Changed));
                OnPropertyChanged();
            }
        }

        public int NSignals => m_signals.Count(i => !i.Removed);

        public bool Removed => m_removed;

        /// <summary>
        /// Indicates whether this Device or any of the associated signals have changed
        /// </summary>
        public bool Changed => m_changed || m_removed || m_signals.Where(item => item.Changed).Any();
        /// <summary>
        /// List of All <see cref="TemplateInputSignal"/> associated with this <see cref="TemplateInputDevice"/>
        /// </summary>
        public ObservableCollection<InputSignalVM> Signals => m_signals;

        /// <summary>
        /// <see cref="RelayCommand"/> to remove this <see cref="TemplateInputDevice"/>
        /// </summary>
        public RelayCommand Remove => m_removeCmd;


        /// <summary>
        /// <see cref="RelayCommand"/> to add a <see cref="TemplateInputSignal"/> to this <see cref="TemplateInputDevice"/>
        /// </summary>
        public RelayCommand AddSignal => m_addSingalCmd;

        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Creates a new <see cref="TemplateInputDevice"/> VieModel
        /// </summary>
        /// <param name="device"> The <see cref="TemplateInputDevice"/> associated with this ViewModel</param>
        /// <param name="DataSourceID">The ID of the <see cref="Template"/> </param>
        public InputDeviceVM(TemplateInputDevice device)
        {
            m_device = device;
            m_signals = new ObservableCollection<InputSignalVM>();
            m_removed = false;
            m_removeCmd = new RelayCommand(Delete, () => true);
            m_addSingalCmd = new RelayCommand(AddNewSignal, () => true);
            m_changed = !(device.ID > 0);
        }

        #endregion

        #region [ Methods ]

        public void Save()
        {
            m_signals.ToList().ForEach(s => s.Save());
            if (m_removed)
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<TemplateInputDevice>(connection).DeleteRecord(m_device);
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                new TableOperations<TemplateInputDevice>(connection).AddNewOrUpdateRecord(m_device);
        }

        public void Delete()
        {
            m_removed = true;
            OnPropertyChanged(nameof(Removed));
            OnPropertyChanged(nameof(Changed));
        }

        public void LoadSignals() 
        {
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                m_signals = new ObservableCollection<InputSignalVM>(new TableOperations<TemplateInputSignal>(connection)
                        .QueryRecordsWhere("DeviceId = {0}", m_device.ID)
                        .Select(d => new InputSignalVM(m_device,d)));

            OnPropertyChanged(nameof(Signals));
            OnPropertyChanged(nameof(NSignals));
            m_signals.ToList().ForEach(s => s.PropertyChanged += SignalChanged);
        }

        private void SignalChanged(object sender, PropertyChangedEventArgs arg)
        {
            if (arg.PropertyName == "Changed")
                OnPropertyChanged("Changed");
            
        }

        private void AddNewSignal()
        {
            string name = "Signal 1";
            int i = 1;
            while (m_signals.Where(d => d.Name == name).Any())
            {
                i++;
                name = "Signal " + i.ToString();
            }

            m_signals.Add(new InputSignalVM(m_device, new TemplateInputSignal() {
                Name = name,
                DeviceID = m_device.ID,
                MeasurmentType=MeasurementType.VoltageMagnitude,
                Phase=Phase.A
            }));

            m_signals.Last().PropertyChanged += SignalChanged;
            OnPropertyChanged(nameof(Signals));
            OnPropertyChanged(nameof(Changed));
            OnPropertyChanged(nameof(NSignals));
        }
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion
    }
}