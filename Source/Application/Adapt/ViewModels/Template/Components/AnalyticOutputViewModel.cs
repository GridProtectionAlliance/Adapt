// ******************************************************************************************************
// AnalyticOutputViewModel.tsx - Gbtc
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
using static Adapt.ViewModels.SelectSignalVM;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for an Analytic Output
    /// </summary>
    public class AnalyticOutputVM : ViewModelBase, ISignalVM
    {
        #region [ Members ]

        private bool m_removed;
        private AnalyticVM m_analytic;
        private AnalyticOutputSignal m_model;
        private InputDeviceVM m_device;
        private TemplateVM m_template;
        private TemplateOutputSignal m_outputModel;
        private bool m_isOutput;
        private string m_outputName;
        private Phase m_phase;
        private MeasurementType m_type;


        private string m_label; // This is coming from the Analytic
        private int m_index;
        private string m_name;

        private int? m_deviceID;
        #endregion

        #region[ Properties ]

        /// <summary>
        /// The Label determined by the <see cref="IAnalytic"/>
        /// </summary>
        public string Label => m_label;
        
        /// <summary>
        /// The Name of this Output signal
        /// </summary>
        public string Name
        {
            get => m_name;
            set
            {
                m_name = value;
                OnPropertyChanged();
            }
        }

        public InputDeviceVM Device
        {
            get => m_device;
            set
            {
                m_device.DeRegisterAnalyticOutput(this);
                m_device.PropertyChanged -= DeviceChanged;
                m_device = value;
                if (m_device != null)
                {
                    m_device.RegisterAnalyticOutput(this);
                    m_device.PropertyChanged += DeviceChanged;
                }
                m_deviceID = m_device?.ID;

                OnPropertyChanged();
            }
        }

        public int OutputIndex {
            get => m_index;
            set
            {
                m_index = value;
                OnPropertyChanged();
            }
        }
        public bool Removed => m_removed;

        /// <summary>
        /// The ID of the Model if available. Null otherwise
        /// </summary>
        public int? ID => m_model?.ID ?? null;

        /// <summary>
        /// Indicates if the Signal is a Template Input Signal
        /// </summary>
        public bool IsInput => false;

        public bool IsOutput
        {
            get => m_isOutput;
            set
            {
                m_isOutput = value;
                OnPropertyChanged();
            }
        }

        public string OutputName
        {
            get => m_outputName;
            set
            {
                m_outputName = value;
                OnPropertyChanged();
            }
        }

        public int SectionOrder => m_analytic.SectionVM.Order;

        public ObservableCollection<InputDeviceVM> AllDevices => new ObservableCollection<InputDeviceVM>(m_template.Devices.Where(item => !item.Removed));
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Creates a new <see cref="AnalyticOutputSignal"/> VieModel
        /// </summary>
        /// <param name="analytic"> The <see cref="AnalyticVM"/> associated with this Output</param>
        /// <param name="analyticOutputSignal">The <see cref="AnalyticOutputSignal"/> for this ViewModel </param>
        /// <param name="label">The Label used for this Output based on the Analytic. </param>
        public AnalyticOutputVM(AnalyticVM analytic, AnalyticOutputSignal model)
        {
            m_removed = false;
            m_analytic = analytic;
            m_model = model;

            m_device = null;

            m_template = analytic.TemplateVM;

            m_label = "";
            m_deviceID = null;


            if (!(m_model is null))
            {
                m_index = m_model.OutputIndex;
                m_name = m_model.Name;
                m_deviceID = m_model.DeviceID;

                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    m_outputModel = new TableOperations<TemplateOutputSignal>(connection).QueryRecordWhere("SignalID = {0} AND IsInputSignal = 0", m_model.ID);
                m_isOutput = !(m_outputModel is null);
                m_outputName = m_outputModel?.Name ?? m_name;
            }

            m_template.PropertyChanged += (object s, PropertyChangedEventArgs args) =>
            {
                if (args.PropertyName == nameof(m_template.Devices))
                    OnPropertyChanged(nameof(AllDevices));
            };

            if (!(m_deviceID is null))
            {
                m_device = m_template.Devices.Where(item => item.ID == m_deviceID).FirstOrDefault();
                if (m_device is null)
                    m_device = m_template.Devices.FirstOrDefault();

                m_deviceID = m_device.ID;
                m_device.RegisterAnalyticOutput(this);
                m_device.PropertyChanged += DeviceChanged;

            }
        }

        /// <summary>
        /// Creates a new <see cref="AnalyticOutputSignal"/> VieModel
        /// </summary>
        /// <param name="analytic"> The <see cref="AnalyticVM"/> associated with this Output</param>
        /// <param name="analyticOutputSignal">The <see cref="AnalyticOutputSignal"/> for this ViewModel </param>
        /// <param name="label">The Label used for this Output based on the Analytic. </param>
        public AnalyticOutputVM(AnalyticVM analytic): this(analytic,null)
        {
            string name = "Signal 1";
            int i = 1;

            m_device = m_template.Devices.Where(d => !d.Removed).FirstOrDefault();
            m_device.RegisterAnalyticOutput(this);
            m_device.PropertyChanged += DeviceChanged;
            m_deviceID = m_device.ID;
            
            if (!(m_device is null))
                while (m_device.Signals.Where(d => d.Name == name && !d.Removed).Any() || m_device.AnalyticSignals.Where(d => d.Name == name && !d.Removed).Any())
                {
                    i++;
                    name = "Signal " + i.ToString();
                }

            m_name = name;
            m_outputName = name;
            OnPropertyChanged(nameof(Device));
        }

        #endregion

        #region [ Methods ]

        public void AnalyticTypeUpdate(IAnalytic instance, int index)
        {
            m_device.DeRegisterAnalyticOutput(this);
            if (instance.Outputs().Count() <= index)
            {

                m_removed = true;
                OnPropertyChanged(nameof(Removed));
                return;
            }

            m_device.RegisterAnalyticOutput(this);
            m_removed = false;
            m_label = instance.Outputs().ElementAt(index).Name;
            m_phase = instance.Outputs().ElementAt(index).Phase;
            m_type = instance.Outputs().ElementAt(index).Type;
            m_index = index;
            OnPropertyChanged(nameof(Removed));
            OnPropertyChanged(nameof(Label));
        }

        public void Save()
        {
            if (m_removed)
                return;

            if (m_model is null)
            {
                m_model = new AnalyticOutputSignal()
                {
                    DeviceID = m_device.ID ?? -1,
                    Name = m_name,
                    AnalyticID = m_analytic?.ID ?? -1,
                    OutputIndex = m_index
                };

                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                {
                    new TableOperations<AnalyticOutputSignal>(connection).AddNewRecord(m_model);
                    m_model.ID = connection.ExecuteScalar<int>("select seq from sqlite_sequence where name = {0}", "AnalyticOutputSignal");
                }

            }
            else
            {

                m_model.Name = m_name;
                m_model.DeviceID = m_device.ID ?? -1;
                m_model.OutputIndex = m_index;
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<AnalyticOutputSignal>(connection).UpdateRecord(m_model);
            }


            // Output Models also need to be saved following the Signal Model
            if (IsOutput)
            {
                if (m_outputModel is null)
                {
                    m_outputModel = new TemplateOutputSignal()
                    {
                        IsInputSignal = false,
                        Name = m_outputName,
                        Phase = (int)m_phase,
                        SignalID = m_model.ID,
                        TemplateID = m_analytic.TemplateVM.ID,
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

        /// <summary>
        /// Delete any unused models form the Database.
        /// </summary>
        public void Delete()
        {
            if (m_model is null)
                return;

            if (m_removed || m_analytic.Removed)
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                new TableOperations<AnalyticOutputSignal>(connection).DeleteRecord(m_model.ID);

            if (!m_isOutput || m_removed || m_analytic.Removed)
            {
                if (m_outputModel == null)
                    return;

                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<TemplateOutputSignal>(connection).DeleteRecord(m_outputModel.ID);
            }
        }

        /// <summary>
        /// Removes this <see cref="AnalyticOutputSignal"/>
        /// </summary>
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
            changed = changed || (m_deviceID is null);

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

            if (string.IsNullOrEmpty(m_name))
                issues.Add($"A name is required for every Output Signal.");

            if (m_device is null)
                issues.Add($"A valid Device needs to be selected");
            else
            {
                int nSig = m_device.Signals.Where(item => item.Name == m_name && !item.Removed).Count();
                nSig += m_device.AnalyticSignals.Where(item => item.Name == m_name  && !item.Removed).Count();
                if (nSig > 1)
                    issues.Add($"A signal with name {m_name} already exists on this Device.");
            }
            

            return issues;
        }

        private void DeviceChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(m_device.Removed) && m_device.Removed)
            {
                m_device.DeRegisterAnalyticOutput(this);
                m_device.PropertyChanged -= DeviceChanged;
                m_device = m_template.Devices.Where(d => !d.Removed).FirstOrDefault();
                if (!(m_device is null))
                {
                    m_device.RegisterAnalyticOutput(this);
                    m_device.PropertyChanged += DeviceChanged;
                    m_deviceID = m_device.ID;
                }

                OnPropertyChanged(nameof(Device));
            }
        }
        /// <summary>
        /// Indicates whether the Analytic can be saved.
        /// </summary>
        /// <returns> <see cref="true"/> If the Analytic can be saved</returns>
        public bool isValid() => Validate().Count() == 0;
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion
    }
}