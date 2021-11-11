// ******************************************************************************************************
//  OutputDeviceViewModel.tsx - Gbtc
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
//  11/11/2021 - C. Lackner
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
using System.Windows.Input;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for a <see cref="TemplateInputDevice"/>
    /// </summary>
    public class OutputDeviceVM: ViewModelBase
    {
        #region [ Members ]

        private TemplateInputDevice m_device;
        private bool m_removed;
        private bool m_selected;
        private bool m_changed;
        private TemplateVM m_templateViewModel;

        #endregion

        #region[ Properties ]
        /// <summary>
        /// The unique ID for the <see cref="TemplateInputDevice"/>
        /// </summary>
        public int ID => m_device.ID;

        /// <summary>
        /// The Name of the Device.
        /// </summary>
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

        /// <summary>
        /// The number of Signals associated with this Device
        /// </summary>
        public int NSignals => Signals.Count(i => !i.Removed);

        /// <summary>
        /// A Flag indicating if this device has been Removed by the User
        /// </summary>
        public bool Removed => m_removed;

        /// <summary>
        /// A Flag indicating whether this Device or any of the associated signals have changed.
        /// This is also true if the Device was Removed.
        /// </summary>
        public bool Changed => m_changed || m_removed || Signals.Where(item => item.Changed).Any();

        /// <summary>
        /// ViewModels for all <see cref="TemplateInputSignal"/> associated with this <see cref="TemplateInputDevice"/>
        /// </summary>
        public ObservableCollection<InputSignalVM> Signals { get; private set; }

        /// <summary>
        /// <see cref="ICommand"/> associated with the remove Button if applicable.
        /// </summary>
        public ICommand Remove { get; }


        /// <summary>
        /// <see cref="ICommand"/> to add a new Signal to this Device.
        /// </summary>
        public ICommand AddSignal { get; }

      
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Creates a new ViewModel for a <see cref="TemplateInputDevice"/>
        /// </summary>
        /// <param name="templateViewModel"> The View model for this <see cref="Template"/> </param>
        /// <param name="device"> The <see cref="TemplateInputDevice"/> associated with this ViewModel</param>
        public OutputDeviceVM(TemplateVM templateViewModel, TemplateInputDevice device)
        {
            m_device = device;
            m_selected = false;
            Signals = new ObservableCollection<InputSignalVM>();
            m_removed = false;
            Remove = new RelayCommand(Delete, () => true);
            AddSignal = new RelayCommand(AddNewSignal, () => true);
            m_changed = !(device.ID > 0);
            m_templateViewModel = templateViewModel;
        }

        #endregion

        #region [ Methods ]

        public void Save()
        {
            Signals.ToList().ForEach(s => s.Save());
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
                Signals = new ObservableCollection<InputSignalVM>(new TableOperations<TemplateInputSignal>(connection)
                        .QueryRecordsWhere("DeviceId = {0}", m_device.ID)
                        .Select(d => new InputSignalVM(null,d)));

            OnPropertyChanged(nameof(Signals));
            OnPropertyChanged(nameof(NSignals));
            Signals.ToList().ForEach(s => s.PropertyChanged += SignalChanged);
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
            while (Signals.Where(d => d.Name == name).Any())
            {
                i++;
                name = "Signal " + i.ToString();
            }

            Signals.Add(new InputSignalVM(null, new TemplateInputSignal()
            {
                Name = name,
                DeviceID = m_device.ID,
                MeasurmentType = MeasurementType.VoltageMagnitude,
                Phase = Phase.A,
                ID = m_templateViewModel.CreateInputSignalID()
            }));

            Signals.Last().PropertyChanged += SignalChanged;
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