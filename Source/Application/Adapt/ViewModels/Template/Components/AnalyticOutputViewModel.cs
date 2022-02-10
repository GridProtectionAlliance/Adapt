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

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for an Analytic Output
    /// </summary>
    public class AnalyticOutputVM : ViewModelBase
    {
        #region [ Members ]

        private bool m_changed;
        private AnalyticVM m_analyticVM;
        private AnalyticOutputSignal m_signal;
        private int m_deviceIndex;
        private ObservableCollection<string> m_Devicenames;
        #endregion

        #region[ Properties ]

        public string Label { get; }
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

        public bool Changed => m_changed;

        public ObservableCollection<string> DeviceNames => m_Devicenames;
        public int DeviceIndex
        {
            get => m_deviceIndex;
            set
            {
                if (value < m_Devicenames.Count() - 1)
                {
                    m_deviceIndex = value;
                    m_signal.DeviceID = m_analyticVM.SectionViewModel.TemplateViewModel.Devices.Where(item => !item.Removed).ToList()[m_deviceIndex].ID;
                }
                else
                    m_signal.DeviceID = m_analyticVM.SectionViewModel.TemplateViewModel.AddDevice(false).ID;
                 
                m_changed = true;
                OnPropertyChanged(nameof(Changed));
                OnPropertyChanged();
            }
        }

        public int DeviceID => m_signal.DeviceID;

        public int OutputIndex => m_signal.OutputIndex;
        public int ID => m_signal.ID;
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Creates a new <see cref="AnalyticOutputSignal"/> VieModel
        /// </summary>
        /// <param name="analytic"> The <see cref="AnalyticVM"/> associated with this Output</param>
        /// <param name="analyticOutputSignal">The <see cref="AnalyticOutputSignal"/> for this ViewModel </param>
        /// <param name="label">The Label used for this Output based on the Analytic. </param>
        public AnalyticOutputVM(AnalyticVM analytic, AnalyticOutputSignal analyticOutputSignal, string label)
        {
            Label = label;
            m_changed = analyticOutputSignal.ID < 1;
            m_analyticVM = analytic;
            m_signal = analyticOutputSignal;
            if (analyticOutputSignal.DeviceID < 1)
            {
                m_deviceIndex = 0;
            }
            else
            {
                m_deviceIndex = m_analyticVM.SectionViewModel.TemplateViewModel.Devices.ToList().FindIndex(d => d.ID == analyticOutputSignal.DeviceID);
            }

            m_analyticVM.SectionViewModel.TemplateViewModel.PropertyChanged += DevicesChanged;
            m_Devicenames = new ObservableCollection<string>(m_analyticVM.SectionViewModel.TemplateViewModel.Devices.ToList().Where(d => !d.Removed).Select(d => d.Name));
            m_Devicenames.Add("Add New Device");
        }

        #endregion

        #region [ Methods ]
        private void DevicesChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "Devices")
            {
                m_Devicenames = new ObservableCollection<string>(m_analyticVM.SectionViewModel.TemplateViewModel.Devices.ToList().Where(d => !d.Removed).Select(d => d.Name));
                m_Devicenames.Add("Add New Device");
                m_deviceIndex = m_analyticVM.SectionViewModel.TemplateViewModel.Devices.ToList().FindIndex(d => d.ID == m_signal.ID);
                if (m_deviceIndex == -1)
                    m_deviceIndex = m_Devicenames.Count() - 1;

                OnPropertyChanged(nameof(DeviceNames));
            }
        }

        public void Save()
        {
            bool removed = m_analyticVM.Removed || m_analyticVM.SectionViewModel.Removed;
            if (!Changed && !removed)
                return;

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            {
                int templateId = new TableOperations<Template>(connection).QueryRecordWhere("Name = {0}", m_analyticVM.SectionViewModel.TemplateViewModel.Name).Id;
                int sectionId = new TableOperations<TemplateSection>(connection).QueryRecordWhere("[Order] = {0} AND TemplateId = {1}", m_analyticVM.SectionViewModel.Order, templateId).ID;
                int analyticID = new TableOperations<Analytic>(connection).QueryRecordWhere("Name = {0} AND TemplateID = {1} AND SectionID = {2}", m_analyticVM.Name, templateId, sectionId).ID;
                int deviceId = new TableOperations<TemplateInputDevice>(connection).QueryRecordWhere("Name = {0} AND TemplateID = {1}",
                    m_analyticVM.SectionViewModel.TemplateViewModel.Devices.ToList().Find(d => d.ID == m_signal.DeviceID).Name, templateId).ID;

                AnalyticOutputSignal sig = new AnalyticOutputSignal()
                {
                    AnalyticID = analyticID,
                    DeviceID = deviceId,
                    Name = m_signal.Name,
                    OutputIndex = m_signal.OutputIndex,
                    ID = (m_signal.ID < -1? 0 : m_signal.ID)
                };

                TableOperations<AnalyticOutputSignal> tbl = new TableOperations<AnalyticOutputSignal>(connection);
                tbl.DeleteRecordWhere("AnalyticID = {0} AND OutputIndex = {1} AND ID <> {2}", analyticID, sig.OutputIndex, sig.ID);

                
                if (!removed)
                    tbl.AddNewOrUpdateRecord(sig);
                if (removed)
                    tbl.DeleteRecord(sig);
            }
        }

        private void ValidateBeforeSave(object sender, CancelEventArgs args)
        {
            if (m_analyticVM.Removed || m_analyticVM.SectionViewModel.Removed)
                return;

            if (m_deviceIndex < 0)
            {
                m_analyticVM.SectionViewModel.TemplateViewModel.AddSaveErrorMessage($"Analytic {m_analyticVM.Name} Output Signal {Name} needs to be attached to a PMU");
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