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
using System.IO;
using System.Linq;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for an Analytic
    /// </summary>
    public class AnalyticVM: ViewModelBase
    {
        #region [ Members ]

        private bool m_removed;
        private bool m_changed;
        private int m_templateID;
        private SectionVM m_SectionVM;
        private Analytic m_analytic;
        private List<TypeDescription> m_analyticTypes;

        private ObservableCollection<AdapterSettingParameterVM> m_settings;
        private ObservableCollection<AnalyticOutputVM> m_outputs;
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
        public ObservableCollection<AnalyticOutputVM> Outputs => m_outputs;

        /// <summary>
        /// The Inputs associated with this Analytic
        /// </summary>
        public ObservableCollection<AnalyticInputVM> Inputs { get; private set; }
        public List<TypeDescription> AnalyticTypes => m_analyticTypes;

        /// <summary>
        /// The Section View model this Analytic is associated with
        /// </summary>
        public SectionVM SectionViewModel => m_SectionVM;
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
                    TypeName = m_analyticTypes[value].Type.FullName;

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
            m_SectionVM = section;
            m_analyticTypes = TypeDescription.LoadDataSourceTypes(FilePath.GetAbsolutePath("").EnsureEnd(Path.DirectorySeparatorChar), typeof(IAnalytic));
            OnAdapterTypeSelectedIndexChanged();
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
                    m_outputs = new ObservableCollection<AnalyticOutputVM>(GetOutputs(Instance));
                    Inputs = new ObservableCollection<AnalyticInputVM>(GetInputs(Instance));
                }
                catch (Exception ex)
                {
                    m_settings = new ObservableCollection<AdapterSettingParameterVM>();
                    m_outputs = new ObservableCollection<AnalyticOutputVM>();
                    Inputs = new ObservableCollection<AnalyticInputVM>();
                }
            }
            else
            {
                m_outputs = new ObservableCollection<AnalyticOutputVM>();
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
            return Instance.OutputNames().Select((n,i) => new AnalyticOutputVM(this, new AnalyticOutputSignal() { 
                AnalyticID=m_analytic.ID, 
                DeviceID=0,
                Name=n,
                OutputIndex=i
            }, n));
        }

        private IEnumerable<AnalyticInputVM> GetInputs(IAnalytic Instance)
        {
            return Instance.InputNames().Select((n, i) => new AnalyticInputVM(this, new AnalyticInput()
            {
                AnalyticID = m_analytic.ID,
            }, n,i));
        }
        private void OnSettingChanged(object sender, SettingChangedArg args)
        {
            m_changed = true;
            OnPropertyChanged("Changed");
        }
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion
    }
}