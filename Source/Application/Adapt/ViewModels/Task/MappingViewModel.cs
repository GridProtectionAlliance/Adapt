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
    public class MappingVM : ViewModelBase
    {
        #region [ Members ]

        private Template m_Template;
        public class Mapping
        {
            public Mapping(TemplateInputDevice device, MappingVM parent)
            {
                TargetDeviceID = device.ID;
                TargetDeviceName = device.Name;
                IsValid = true;
                ChangeDevice = new RelayCommand(() => parent.SelectDevice(this), () => true);
                FixChannel = new RelayCommand(() => parent.FixDuplicates(this), () => true);

                ChannelMappings = new Dictionary<int, string>();
            }
            public int TargetDeviceID { get; set; }
            public string SourceDeviceID { get; set; }
            public string TargetDeviceName { get; set; }
            public string SourceDeviceName { get; set; }
            public ICommand ChangeDevice { get; }
            public ICommand FixChannel { get; }
            public Dictionary<int,string> ChannelMappings { get; set; }
            public bool IsValid { get; set; }
        }

        #endregion

        #region[ Properties ]

        
        /// <summary>
        /// The index of the DataSource Selected.
        /// </summary>
        public IDataSource DataSource 
        { 
            get;
            set;
        }

        public DataSource DataSourceModel { get; set; }
        public ObservableCollection<Mapping> DeviceMappings { get; set; }

        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates a New <see cref="MappingVM"/>
        /// </summary>
        /// <param name="template"> The corresponding <see cref="Template"/></param>
        public MappingVM(Template template)
        {
            m_Template = template;
            LoadTargetDevices();
        }

        #endregion

        #region [ Methods ]
        
        /// <summary>
        /// Loads the Devices associated with this Template.
        /// </summary>
        private void LoadTargetDevices()
        {
            DeviceMappings = new ObservableCollection<Mapping>();

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                DeviceMappings = new ObservableCollection<Mapping>(
                    new TableOperations<TemplateInputDevice>(connection).QueryRecordsWhere("TemplateID = {0}", m_Template.Id)
                    .Select(d=> new Mapping(d, this)).ToList()
                    );

            OnPropertyChanged(nameof(DeviceMappings));
        }

        /// <summary>
        /// Opens the Select Device Window
        /// </summary>
        private void SelectDevice(Mapping mapping)
        {
            SelectSignal dateSelection = new SelectSignal();
            SelectSignalMappingVM<AdaptDevice> dateSelectionVM = new SelectSignalMappingVM<AdaptDevice>((d) => {
                mapping.SourceDeviceID = d.ID;
                mapping.SourceDeviceName = d.Name;

                mapping.ChannelMappings = new Dictionary<int, string>();

                // Validate Signals on that device
                List<int> targetSignals;
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    targetSignals = new TableOperations<TemplateInputSignal>(connection)
                        .QueryRecordsWhere("DeviceID = {0}", mapping.TargetDeviceID).Select(s => s.ID).ToList();
                    
                List<AdaptSignal> sourceSignals = AdaptSignal.Get(DataSource, DataSourceModel.ID, ConnectionString, DataProviderString)
                    .Where(s => s.Device == d.ID).ToList();

                mapping.IsValid = sourceSignals.Count() >= targetSignals.Count();
                for (int i = 0; i < targetSignals.Count(); i++)
                    if (i < sourceSignals.Count())
                        mapping.ChannelMappings.Add(targetSignals[i], sourceSignals[i].ID);
                    else
                        mapping.ChannelMappings.Add(targetSignals[i],"");
                DeviceMappings = new ObservableCollection<Mapping>(DeviceMappings);
                OnPropertyChanged(nameof(DeviceMappings));
            },(d,s) => d.Name.ToLower().Contains(s.ToLower()),(d) => d.Name,AdaptDevice.Get(DataSource,DataSourceModel.ID,ConnectionString,DataProviderString));
            dateSelection.DataContext = dateSelectionVM;
            dateSelection.Show();
        }

        /// <summary>
        /// Opens Window to select signals from a device.
        /// </summary>
        public void FixDuplicates(Mapping mapping)
        {
            if (mapping.ChannelMappings.Count() == 0 || !mapping.ChannelMappings.ContainsValue(""))
                return;

            int targetId = mapping.ChannelMappings.First(item => item.Value == "").Key;
            string title = "Select a Signal for {0}";
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                title += new TableOperations<TemplateInputSignal>(connection)
                    .QueryRecordWhere("Id = {0}", targetId)?.Name ?? "";

            SelectSignal signalSelection = new SelectSignal();
            SelectSignalMappingVM<AdaptSignal> dateSelectionVM = new SelectSignalMappingVM<AdaptSignal>((d) => {
                mapping.ChannelMappings[targetId] = d.ID;
                mapping.IsValid = !mapping.ChannelMappings.ContainsValue("");
                DeviceMappings = new ObservableCollection<Mapping>(DeviceMappings);
                OnPropertyChanged(nameof(DeviceMappings));
            }, (d, s) => d.Name.ToLower().Contains(s.ToLower()), (d) => d.Name, AdaptSignal.Get(DataSource, DataSourceModel.ID, ConnectionString, DataProviderString)
            .Where(s => s.ID == mapping.SourceDeviceID), title);
            signalSelection.DataContext = dateSelectionVM;
            signalSelection.Show();
            
        }
        /// <summary>
        /// Updates the DataSource model and instance
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="model"></param>
        public void UpdateDataSource(IDataSource instance, DataSource model)
        {
            DataSource = instance;
            DataSourceModel = model;

            OnPropertyChanged(nameof(DataSource));
            OnPropertyChanged(nameof(DataSourceModel));
        }

        
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        
        #endregion
    }
}