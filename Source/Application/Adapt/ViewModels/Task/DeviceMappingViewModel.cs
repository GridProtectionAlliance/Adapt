// ******************************************************************************************************
//  MappingViewModel.tsx - Gbtc
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
//  09/07/2021 - C. Lackner
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
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Transactions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for Task Window
    /// </summary>

    public class DeviceMapping : ViewModelBase
    {
        #region [ Member ]
        public class SignalMapping
        {
            private TemplateInputSignal m_targetSignal;
            private DeviceMapping m_parent;

            public SignalMapping(DeviceMapping parent, TemplateInputSignal templateSignal, AdaptSignal signal)
            {
                SourceChannel = signal;
                m_parent = parent;
                m_targetSignal = templateSignal;
                SelectChannel = new RelayCommand(SelectSignal, () => true);
            }

            public ICommand SelectChannel { get; }
            public string TargetChannelName => m_targetSignal.Name;
            public AdaptSignal SourceChannel { get; set; }
            public int TargetChannelID => m_targetSignal.ID;
            public string SourceChannelName => SourceChannel?.Name ?? "";

            public bool IsValid => !(SourceChannel is null);

            private void SelectSignal()
            {


                string title = "Select a Signal for " + TargetChannelName ?? "";

                SelectSignal signalSelection = new SelectSignal();

                SelectMappingVM<AdaptSignal> selectionVM = new SelectMappingVM<AdaptSignal>((d) => {
                    SourceChannel = d.FirstOrDefault();

                    m_parent.SignalChangedCallback();
                    signalSelection.Close();
                }, (d, s) => d.Name.ToLower().Contains(s.ToLower()), (d) => d.Name, AdaptSignal.Get(m_parent.Parent.Parent.DataSourceInstance, m_parent.Parent.DataSource.ID, ConnectionString, DataProviderString)
               .Where(s => s.Device == m_parent.SourceDevice.ID && s.Phase == m_targetSignal.Phase && s.Type == m_targetSignal.MeasurmentType), title);
                signalSelection.DataContext = selectionVM;
                signalSelection.ShowDialog();
            }

        }

        private MappingVM m_parent;
        private TemplateInputDevice m_targetDevice;
        #endregion

        #region [ Constructor ]
        public DeviceMapping(TemplateInputDevice device, MappingVM parent)
        {
            m_targetDevice = device;
            m_parent = parent;
            IsSelected = false;
            ChangeDevice = new RelayCommand(SelectDevice, () => true);
            ChannelMappings = new ObservableCollection<SignalMapping>();
        }
        #endregion

        #region [ Properties ]
        public int TargetDeviceID => m_targetDevice.ID;
        public IDevice SourceDevice { get; set; }
        public string TargetDeviceName => m_targetDevice.Name;

        public string SourceDeviceName => SourceDevice?.Name ?? "";
        public ICommand ChangeDevice { get; }
        public ObservableCollection<SignalMapping> ChannelMappings { get; set; }
        
        public Action<SignalMapping> FixChannel { get; }
        public bool IsValid => !ChannelMappings.Any(item => !item.IsValid);
        public bool IsSelected { get; set; }

        public MappingVM Parent => m_parent;
        #endregion

        #region [ Methods ]

        private void SelectDevice()
        {

            SelectSignal selectionWindow = new SelectSignal();

            SelectMappingVM<AdaptDevice> deviceSelectionVM = new SelectMappingVM<AdaptDevice>((d) => {
                AssignDevice(d.FirstOrDefault());
                selectionWindow.Close();
            }, (d, s) => d.Name.ToLower().Contains(s.ToLower()), (d) => d.Name, AdaptDevice.Get(m_parent.Parent.DataSourceInstance, m_parent.DataSource.ID, ConnectionString, DataProviderString));;
            selectionWindow.DataContext = deviceSelectionVM;
            selectionWindow.ShowDialog();
        }

        public void SignalChangedCallback()
        {
            ChannelMappings = new ObservableCollection<SignalMapping>(ChannelMappings);
            OnPropertyChanged(nameof(ChannelMappings));
            OnPropertyChanged(nameof(IsValid));
            m_parent.DeviceChangedCallback();
        }
      
        public void AssignDevice(AdaptDevice device)
        {
            SourceDevice = device;
            ChannelMappings = new ObservableCollection<SignalMapping>();

            // Validate Signals on that device
            List<TemplateInputSignal> targetSignals;
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                targetSignals = new TableOperations<TemplateInputSignal>(connection)
                    .QueryRecordsWhere("DeviceID = {0}", TargetDeviceID).ToList();

            List<AdaptSignal> sourceSignals = AdaptSignal.Get(m_parent.Parent.DataSourceInstance, m_parent.Parent.DataSource.ID, ConnectionString, DataProviderString)
                .Where(s => s.Device == SourceDevice.ID).ToList();

            for (int i = 0; i < targetSignals.Count(); i++)
            {
                int c = sourceSignals.Count(item => item.Phase == targetSignals[i].Phase && item.Type == targetSignals[i].MeasurmentType);

                if (c != 1)
                    ChannelMappings.Add(new SignalMapping(this, targetSignals[i], null));
                else
                    ChannelMappings.Add(new SignalMapping(this, targetSignals[i], sourceSignals.Find(item => item.Phase == targetSignals[i].Phase && item.Type == targetSignals[i].MeasurmentType)));
            }


            IsSelected = true;

            m_parent.DeviceMappings = new ObservableCollection<DeviceMapping>(m_parent.DeviceMappings);

            m_parent.DeviceChangedCallback();
            OnPropertyChanged(nameof(ChannelMappings));
            OnPropertyChanged(nameof(IsValid));
        }
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";

        #endregion
    }
}