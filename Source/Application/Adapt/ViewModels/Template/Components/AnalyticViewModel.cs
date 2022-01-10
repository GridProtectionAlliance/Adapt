// ******************************************************************************************************
// AnalyticViewModel.tsx - Gbtc
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
using System.Reflection;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for an Analytic
    /// </summary>
    public class AnalyticVM : ViewModelBase
    {
        #region [ Members ]

        private bool m_removed;
        private bool m_changed;
        private Analytic m_analytic;
        private List<TypeDescription> m_analyticTypes;

        private ObservableCollection<AdapterSettingParameterVM> m_settings;
        #endregion

        #region[ Properties ]

        public string Name
        {
            get => m_analytic.Name;
            set
            {
                m_analytic.Name = value;
                OnPropertyChanged();
                m_changed = true;
                OnPropertyChanged(nameof(Changed));
            }
        }

        public int ID => m_analytic.ID;
        public bool Changed => m_changed || m_removed;
        public bool Removed => m_removed;

        public ObservableCollection<AdapterSettingParameterVM> Settings => m_settings;

        /// <summary>
        /// The Outputs of this <see cref="Analytic"/>.
        /// </summary>
        public ObservableCollection<AnalyticOutputVM> Outputs { get; private set; }

        /// <summary>
        /// The Inputs associated with this Analytic
        /// </summary>
        public ObservableCollection<AnalyticInputVM> Inputs { get; private set; }
        public List<TypeDescription> AnalyticTypes => m_analyticTypes;

        /// <summary>
        /// The Section View model this Analytic is associated with
        /// </summary>
        public SectionVM SectionViewModel { get; set; }
        /// <summary>
        /// Gets or sets the index of the selected item in the <see cref="AnalyticTypes"/>.
        /// </summary>
        public int AdapterTypeSelectedIndex
        {
            get
            {
                int index = m_analyticTypes
                    .Select(tuple => tuple.Type)
                    .TakeWhile(type => type.FullName != TypeName)
                    .Count();

                if (index == m_analyticTypes.Count)
                    index = -1;

                return index;
            }
            set
            {
                if (value >= 0 && value < m_analyticTypes.Count)
                {
                    TypeName = m_analyticTypes[value].Type.FullName;
                    m_analytic.AssemblyName = m_analyticTypes[value].Type.Assembly.FullName;
                }

                OnAdapterTypeSelectedIndexChanged();

            }
        }

        public string TypeName
        {
            get => m_analytic?.TypeName ?? "";
            set
            {
                m_analytic.TypeName = value;
                OnPropertyChanged();
                m_changed = true;
                OnPropertyChanged(nameof(Changed));
            }
        }

        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Creates a new <see cref="TemplateInputDevice"/> VieModel
        /// </summary>
        /// <param name="analytic">The <see cref="Analytic"/> for this ViewModel </param>
        /// <param name="section">The <see cref="SectionVM"/> associated with this <see cref="Analytic"/> </param>
        public AnalyticVM(Analytic analytic, SectionVM section)
        {
            m_removed = false;
            m_changed = analytic.ID < 1;
            m_analytic = analytic;
            m_settings = new ObservableCollection<AdapterSettingParameterVM>();
            SectionViewModel = section;
            Outputs = new ObservableCollection<AnalyticOutputVM>();
            Inputs = new ObservableCollection<AnalyticInputVM>();
            m_analyticTypes = TypeDescription.LoadDataSourceTypes(FilePath.GetAbsolutePath("").EnsureEnd(Path.DirectorySeparatorChar), typeof(IAnalytic));
            m_analyticTypes = m_analyticTypes.Where(isAllowed).ToList();
            OnAdapterTypeSelectedIndexChanged();
            SectionViewModel.TemplateViewModel.BeforeSave += ValidateBeforeSave;
        }

        #endregion

        #region [ Methods ]

        private void OnAdapterTypeSelectedIndexChanged()
        {
            OnPropertyChanged(nameof(AdapterTypeSelectedIndex));
            OnPropertyChanged(nameof(TypeName));

            if (AdapterTypeSelectedIndex >= 0 && AdapterTypeSelectedIndex < m_analyticTypes.Count)
            {
                try
                {

                    IAnalytic Instance = (IAnalytic)Activator.CreateInstance(m_analyticTypes[AdapterTypeSelectedIndex].Type);
                    m_settings = new ObservableCollection<AdapterSettingParameterVM>(AdapterSettingParameterVM.GetSettingParameters(Instance, m_analytic?.ConnectionString ?? ""));
                    m_settings.ToList().ForEach(s => s.SettingChanged += OnSettingChanged);
                    
                    Outputs.ToList().ForEach(item => item.RemoveErrorMessages());
                    Inputs.ToList().ForEach(item => item.RemoveErrorMessages());
                    Outputs = new ObservableCollection<AnalyticOutputVM>(GetOutputs(Instance));
                    Inputs = new ObservableCollection<AnalyticInputVM>(GetInputs(Instance));
                }
                catch (Exception ex)
                {
                    m_settings = new ObservableCollection<AdapterSettingParameterVM>();
                    Outputs = new ObservableCollection<AnalyticOutputVM>();
                    Inputs = new ObservableCollection<AnalyticInputVM>();
                }
            }
            else
            {
                Outputs = new ObservableCollection<AnalyticOutputVM>();
                m_settings = new ObservableCollection<AdapterSettingParameterVM>();
                Inputs = new ObservableCollection<AnalyticInputVM>();
            }
            OnPropertyChanged(nameof(Settings));
            OnPropertyChanged(nameof(Outputs));
            m_changed = true;
            OnPropertyChanged(nameof(Changed));
            OnPropertyChanged(nameof(Inputs));
        }

        private IEnumerable<AnalyticOutputVM> GetOutputs(IAnalytic Instance)
        {
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            {
                TableOperations<AnalyticOutputSignal> tbl = new TableOperations<AnalyticOutputSignal>(connection);
                return Instance.OutputNames().Select((n, i) =>
                {
                    int ct = tbl.QueryRecordCountWhere("AnalyticID = {0} AND OutputIndex = {1}", m_analytic.ID, i);
                    if (ct > 0)
                        return new AnalyticOutputVM(this, tbl.QueryRecordWhere("AnalyticID = {0} AND OutputIndex = {1}", m_analytic.ID, i), n);

                    return new AnalyticOutputVM(this, new AnalyticOutputSignal()
                    {
                        AnalyticID = m_analytic.ID,
                        DeviceID = SectionViewModel.TemplateViewModel.Devices.First().ID,
                        Name = n,
                        OutputIndex = i
                    }, n);
                }).ToList();
            }
        }

        private IEnumerable<AnalyticInputVM> GetInputs(IAnalytic Instance)
        {
            List<AnalyticInputVM> inputs = Inputs.ToList();

            return Instance.InputNames().Select((n, i) => {
                if (inputs.FindIndex(item => item.Label == n) > -1)
                    return inputs.Find(item => item.Label == n);
                return new AnalyticInputVM(this, new AnalyticInput()
                {
                    AnalyticID = m_analytic.ID,
                    InputIndex = i
                }, n, i);
                });
        
        }
        private void OnSettingChanged(object sender, SettingChangedArg args)
        {
            m_changed = true;
            OnPropertyChanged("Changed");
        }

        /// <summary>
        /// Saves this Analytic and all associated inputs and outputs.
        /// </summary>
        public void Save()
        {
            if (!Changed)
                return;

            IAnalytic Instance = (IAnalytic)Activator.CreateInstance(m_analyticTypes[AdapterTypeSelectedIndex].Type);
            m_analytic.ConnectionString = AdapterSettingParameterVM.GetConnectionString(m_settings.ToList(), Instance);

            bool removed = m_removed || SectionViewModel.Removed;

            if (removed)
            {
                Outputs.ToList().ForEach(o => o.Save());
                Inputs.ToList().ForEach(i => i.Save());
            }
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            {
                TableOperations<Analytic> analyticTbl = new TableOperations<Analytic>(connection);
                if (m_analytic.ID < 0 && !removed)
                {
                    int templateId = new TableOperations<Template>(connection).QueryRecordWhere("Name = {0}", SectionViewModel.TemplateViewModel.Name).Id;
                    int sectionId = new TableOperations<TemplateSection>(connection).QueryRecordWhere("[Order] = {0} AND TemplateId = {1}", SectionViewModel.Order, templateId).ID;
                    analyticTbl.AddNewRecord(new Analytic()
                    {
                        Name = m_analytic.Name,
                        AssemblyName = m_analytic.AssemblyName,
                        ConnectionString = m_analytic.ConnectionString,
                        TypeName = m_analytic.TypeName,
                        SectionID = sectionId,
                        TemplateID = templateId
                    });
                }
                else if (removed)
                    analyticTbl.DeleteRecord(m_analytic);
                else
                    analyticTbl.UpdateRecord(m_analytic);

                if (!removed)
                {
                    Outputs.ToList().ForEach(o => o.Save());
                    Inputs.ToList().ForEach(i => i.Save());
                }
            }

        }

        /// <summary>
        /// Loads all Inputs associated with this <see cref="Analytic"/>.
        /// </summary>
        public void LoadInputs()
        {
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            {
                try
                {
                    IAnalytic Instance = (IAnalytic)Activator.CreateInstance(m_analyticTypes[AdapterTypeSelectedIndex].Type);

                    List<AnalyticInput> savedInputs = new TableOperations<AnalyticInput>(connection).QueryRecordsWhere("AnalyticID = {0}", m_analytic.ID).ToList();
                    Inputs.ToList().ForEach(item => item.RemoveErrorMessages());
                    Inputs = new ObservableCollection<AnalyticInputVM>(Instance.InputNames().Select((n, i) =>
                   {
                       if (savedInputs.FindIndex(item => item.InputIndex == i) > -1)
                           return new AnalyticInputVM(this, savedInputs.Find(item => item.InputIndex == i), n, i);
                       return new AnalyticInputVM(this, new AnalyticInput()
                       {
                           AnalyticID = m_analytic.ID,
                           InputIndex = i
                       }, n, i);
                   }));
                }
                catch
                {
                    Inputs = new ObservableCollection<AnalyticInputVM>();
                }                
            }
            OnPropertyChanged(nameof(Inputs));
        }

        /// <summary>
        /// Loads all Outputs associated with this <see cref="Analytic"/>
        /// </summary>
        public void LoadOutputs()
        {
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            {
                try
                {
                    IAnalytic Instance = (IAnalytic)Activator.CreateInstance(m_analyticTypes[AdapterTypeSelectedIndex].Type);

                    List<AnalyticOutputSignal> savedOutputs = new TableOperations<AnalyticOutputSignal>(connection).QueryRecordsWhere("AnalyticID = {0}", m_analytic.ID).ToList();
                    Outputs.ToList().ForEach(item => item.RemoveErrorMessages());
                    Outputs = new ObservableCollection<AnalyticOutputVM>(Instance.OutputNames().Select((n, i) =>
                    {
                        if (savedOutputs.FindIndex(item => item.OutputIndex == i) > -1)
                            return new AnalyticOutputVM(this, savedOutputs.Find(item => item.OutputIndex == i), n);
                        return new AnalyticOutputVM(this, new AnalyticOutputSignal()
                        {
                            AnalyticID = m_analytic.ID,
                            OutputIndex = i,
                            Name = n,
                            DeviceID = SectionViewModel.TemplateViewModel.Devices.FirstOrDefault()?.ID ?? 0
                        }, n);
                    }));
                }
                catch
                {
                    Outputs = new ObservableCollection<AnalyticOutputVM>();
                }
            }
            OnPropertyChanged(nameof(Outputs));
        }

        /// <summary>
        /// Removes this <see cref="Analytic"/>
        /// </summary>
        private void Delete()
        {
            m_analytic = null;
        }

        /// <summary>
        /// Determines if the TypeDescription given is allowed on the IAnalytic
        /// </summary>
        /// <param name="typeDescription"></param>
        /// <returns></returns>
        private bool isAllowed(TypeDescription typeDescription)
        {
            AnalyticSectionAttribute[] analytic = (AnalyticSectionAttribute[])typeDescription.Type.GetCustomAttributes(typeof(AnalyticSectionAttribute), false);
            AnalyticSection[] m = analytic.SelectMany(item => item.Sections).ToArray();
            return m.Contains(SectionViewModel.AnalyticSection);
        }

        private void ValidateBeforeSave(object sender, CancelEventArgs args)
        {
            if (AdapterTypeSelectedIndex < 0 || AdapterTypeSelectedIndex >= m_analyticTypes.Count)
            {
                SectionViewModel.TemplateViewModel.AddSaveErrorMessage($"Analytic {Name} does not have a valid Type");
                args.Cancel = true;
            }
        }
        #endregion
        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion

    }
}