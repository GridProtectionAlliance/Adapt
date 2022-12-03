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
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for an Analytic
    /// </summary>
    public class AnalyticVM : ViewModelBase
    {
        #region [ Members ]

        private bool m_removed;

        private Analytic m_model;
        private SectionVM m_parent;

        private string m_name;
        private string m_typeName;
        private string m_assemblyName;
        private string m_connectionString;

        private ObservableCollection<AdapterSettingParameterVM> m_settings;
        private ObservableCollection<AnalyticOutputVM> m_outputs;
        private ObservableCollection<AnalyticInputVM> m_inputs;
        private IAnalytic m_instance;

        #endregion

        #region[ Properties ]

        public TemplateVM TemplateVM => m_parent.TemplateVM;
        public SectionVM SectionVM => m_parent;

        public string Name
        {
            get => m_name ?? m_model?.Name ?? "";
            set
            {
                m_name = value;
                OnPropertyChanged();
            }
        }

        public bool Removed => m_removed;

        public ObservableCollection<AdapterSettingParameterVM> Settings => m_settings;

        /// <summary>
        /// The Outputs of this <see cref="Analytic"/>.
        /// </summary>
        public ObservableCollection<AnalyticOutputVM> Outputs => m_outputs;

        /// <summary>
        /// The Inputs associated with this Analytic
        /// </summary>
        public ObservableCollection<AnalyticInputVM> Inputs => m_inputs;

        /// <summary>
        /// List of available <see cref="IAnalytic"/>
        /// </summary>
        public List<TypeDescription> AnalyticTypes => m_analyticTypes.Where(isAllowed).ToList();
            
        /// <summary>
        /// Gets or sets the index of the selected item in the <see cref="AnalyticTypes"/>.
        /// </summary>
        public int AdapterTypeSelectedIndex
        {
            get
            {
                int index = AnalyticTypes
                    .Select(tuple => tuple.Type)
                    .TakeWhile(type => type.FullName != TypeName)
                    .Count();

                if (index == AnalyticTypes.Count)
                    index = -1;

                return index;
            }
            set
            {
                if (value >= 0 && value < m_analyticTypes.Count)
                {
                    m_typeName = AnalyticTypes[value].Type.FullName;
                    m_assemblyName = AnalyticTypes[value].Type.Assembly.FullName;
                }

                OnAdapterTypeSelectedIndexChanged();

            }
        }

        public string TypeName
        {
            get => m_typeName ?? m_model?.TypeName ?? "";
            set
            {
                m_typeName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The ID of the associated Model.
        /// Note that this will be <see cref="Nullable"/> and is NULL if it's a new Model
        /// </summary>
        public int? ID => m_model?.ID ?? null;

        /// <summary>
        /// Command to Remove Analytic
        /// </summary>
        public ICommand DeleteAnalyticCommand { get; }

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
            
            m_model = analytic;
            m_parent = section;

            m_settings = new ObservableCollection<AdapterSettingParameterVM>();
            m_outputs = new ObservableCollection<AnalyticOutputVM>();
            m_inputs = new ObservableCollection<AnalyticInputVM>();
            m_instance = null;

            if (!(analytic is null))
            {
                m_name = analytic.Name;
                m_typeName = analytic.TypeName;
                m_assemblyName = analytic.AssemblyName;
                m_connectionString = analytic.ConnectionString;
                m_instance = (IAnalytic)Activator.CreateInstance(AnalyticTypes[AdapterTypeSelectedIndex].Type);
            }

            if (!(m_instance is null))
            {
                m_settings = new ObservableCollection<AdapterSettingParameterVM>(AdapterSettingParameterVM.GetSettingParameters(m_instance, m_connectionString));
                m_settings.ToList().ForEach(s => s.PropertyChanged += SettingChange);
                m_connectionString = AdapterSettingParameterVM.GetConnectionString(Settings.ToList(), m_instance);
            }

            if (!(m_model is null))
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                {
                    List<AnalyticOutputSignal> savedOutputs = new TableOperations<AnalyticOutputSignal>(connection)
                        .QueryRecordsWhere("AnalyticID = {0}", m_model.ID).OrderBy(o => o.OutputIndex).ToList();
                    List<AnalyticInput> savedInputs = new TableOperations<AnalyticInput>(connection)
                        .QueryRecordsWhere("AnalyticID = {0}", m_model.ID).OrderBy(o => o.InputIndex).ToList();

                    List<AnalyticOutputVM> actualOutputs = new List<AnalyticOutputVM>();
                    List<AnalyticInputVM> actualInputs= new List<AnalyticInputVM>();

                    int processedIndex = 0;
                    int currentOrder = -1;
                    while (processedIndex < savedOutputs.Count)
                    {
                        currentOrder++;
                        if (currentOrder == savedOutputs[processedIndex].OutputIndex)
                        {
                            actualOutputs.Add(new AnalyticOutputVM(this,savedOutputs[processedIndex]));
                            processedIndex++;
                        }
                        else
                            actualOutputs.Add(new AnalyticOutputVM(this));

                        if (processedIndex < savedOutputs.Count && currentOrder > savedOutputs[processedIndex].OutputIndex)
                            processedIndex++;
                    }
                    processedIndex = 0;
                    currentOrder = -1;
                    while (processedIndex < savedInputs.Count)
                    {
                       
                        currentOrder++;
                        if (currentOrder == savedInputs[processedIndex].InputIndex)
                        {
                            actualInputs.Add(new AnalyticInputVM(this, savedInputs[processedIndex]));
                            processedIndex++;
                        }
                        else
                            actualInputs.Add(new AnalyticInputVM(this));

                        if (processedIndex < savedInputs.Count && currentOrder > savedInputs[processedIndex].InputIndex)
                            processedIndex++;
                    }

                    m_outputs = new ObservableCollection<AnalyticOutputVM>(actualOutputs);
                    m_inputs = new ObservableCollection<AnalyticInputVM>(actualInputs);
                }

            UpdateSignals();
            DeleteAnalyticCommand = new RelayCommand(Removal, () => true);
        }

        /// <summary>
        /// Creates a new <see cref="TemplateInputDevice"/> VieModel
        /// </summary>
        /// <param name="analytic">The <see cref="Analytic"/> for this ViewModel </param>
        /// <param name="section">The <see cref="SectionVM"/> associated with this <see cref="Analytic"/> </param>
        public AnalyticVM(SectionVM section): this(null,section)
        {
            string name = "Analytic 1";
            int i = 1;
            while (section.Analytics.Where(d => d.Name == name && !d.Removed).Any())
            {
                i++;
                name = "Analytic " + i.ToString();
            }

            m_name = name;
            AdapterTypeSelectedIndex = 0;
            m_instance = (IAnalytic)Activator.CreateInstance(AnalyticTypes[AdapterTypeSelectedIndex].Type);
            m_connectionString = "";

            if (m_instance is null)
                m_settings = new ObservableCollection<AdapterSettingParameterVM>();
            else
            {
                m_settings = new ObservableCollection<AdapterSettingParameterVM>(AdapterSettingParameterVM.GetSettingParameters(m_instance, m_connectionString));
                m_settings.ToList().ForEach(s => s.PropertyChanged += SettingChange);
                m_connectionString = AdapterSettingParameterVM.GetConnectionString(Settings.ToList(), m_instance);
            }
            UpdateSignals();
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Settings));
            OnPropertyChanged(nameof(Outputs));
            OnPropertyChanged(nameof(Inputs));
        }

        #endregion

        #region [ Methods ]

        private void OnAdapterTypeSelectedIndexChanged()
        {

            OnPropertyChanged(nameof(AdapterTypeSelectedIndex));
            OnPropertyChanged(nameof(TypeName));


            if (AdapterTypeSelectedIndex >= 0 && AdapterTypeSelectedIndex < AnalyticTypes.Count)
            {
                try
                {

                    m_instance = (IAnalytic)Activator.CreateInstance(AnalyticTypes[AdapterTypeSelectedIndex].Type);
                    m_settings = new ObservableCollection<AdapterSettingParameterVM>(AdapterSettingParameterVM.GetSettingParameters(m_instance, m_connectionString));
                    m_settings.ToList().ForEach(s => s.PropertyChanged += SettingChange);
                    UpdateSignals();


                }
                catch (Exception ex)
                {
                    m_settings = new ObservableCollection<AdapterSettingParameterVM>();
                }
            }
           
            OnPropertyChanged(nameof(Settings));
            OnPropertyChanged(nameof(Outputs));
            OnPropertyChanged(nameof(Inputs));
        }
 
        /// <summary>
        /// Saves this Analytic and all associated inputs and outputs.
        /// </summary>
        public void Save()
        {

            if (m_removed)
                return;

            if (m_instance != null)
                m_connectionString = AdapterSettingParameterVM.GetConnectionString(Settings.ToList(), m_instance);

            if (m_model is null)
            {
               
                m_model = new Analytic()
                {
                    Name = m_name,            
                    TemplateID = m_parent.TemplateVM.ID,
                    SectionID = m_parent.ID ?? -1,
                    TypeName = m_typeName,
                    ConnectionString = m_connectionString,
                    AssemblyName = m_assemblyName
                 };

                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                {
                    new TableOperations<Analytic>(connection).AddNewRecord(m_model);
                    m_model.ID = connection.ExecuteScalar<int>("select seq from sqlite_sequence where name = {0}", "Analytic");
                }

            }
            else
            {
                m_model.Name = m_name;
                m_model.TypeName = m_typeName;
                m_model.AssemblyName = m_assemblyName;
                m_model.ConnectionString = m_connectionString;
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<Analytic>(connection).UpdateRecord(m_model);
            }

            foreach (AnalyticInputVM i in Inputs)
                i.Save();
            foreach (AnalyticOutputVM o in Outputs)
                o.Save();

        }

        /// <summary>
        /// Delete any unused models form the Database.
        /// </summary>
        public void Delete()
        { 
            
            foreach (AnalyticInputVM i in Inputs)
                i.Delete();
            foreach (AnalyticOutputVM o in Outputs)
                o.Delete();

            if (!m_removed && !m_parent.Removed)
                return;

            if (m_model is null)
                return;

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                new TableOperations<Analytic>(connection).DeleteRecord(m_model.ID);
        }

        /// <summary>
        /// Loads all Signals associated with this <see cref="Analytic"/>.
        /// </summary>
        private void UpdateSignals()
        {
            if (m_instance == null)
                return;

            int i = 0;
            foreach (AnalyticOutputVM o in m_outputs)
            {
                o.AnalyticTypeUpdate(m_instance, i);
                i++;
            }

            while (m_outputs.Count() < m_instance.Outputs().Count())
            {
                AnalyticOutputVM o = new AnalyticOutputVM(this);
                o.AnalyticTypeUpdate(m_instance, i);
                i++;
                m_outputs.Add(o);
            }

            i = 0;
            foreach (AnalyticInputVM inp in m_inputs)
            {
                inp.AnalyticTypeUpdate(m_instance, i);
                i++;
            }

            while (m_inputs.Count() < m_instance.InputNames().Count())
            {
                AnalyticInputVM inp = new AnalyticInputVM(this);
                inp.AnalyticTypeUpdate(m_instance, i);
                i++;
                m_inputs.Add(inp);
            }

            OnPropertyChanged(nameof(Outputs));
            OnPropertyChanged(nameof(Inputs));
        }

        /// <summary>
        /// Removes this <see cref="Analytic"/>
        /// </summary>
        public void Removal()
        {
            m_removed = true;
            foreach (AnalyticInputVM i in Inputs)
                i.Removal();
            foreach (AnalyticOutputVM o in Outputs)
                o.Removal();

            OnPropertyChanged(nameof(Removed));
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
            return m.Contains(m_parent.AnalyticSection);
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
            changed = changed || m_model.TypeName != m_typeName;
            changed = changed || m_model.AssemblyName != m_assemblyName;
            
            changed = changed || (m_model.ConnectionString != m_connectionString);
            changed = changed || m_outputs.Any(o => o.HasChanged());
            changed = changed || m_inputs.Any(o => o.HasChanged());
            changed = changed || m_removed;

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
                issues.Add($"A name is required for every Analytic.");

            int nDev = m_parent.Analytics.Where(item => item.Name == m_name).Count();
            if (nDev > 1)
                issues.Add($"An analytic with name {m_name} already exists.");

            if (m_instance is null)
                issues.Add($"Unable to generate Analytic of type {m_typeName}");

            issues.AddRange(m_settings.Where(s => s.IsInvalid).Select(s => $"{m_name} Setting {s.Name} is invalid."));
            issues.AddRange(m_outputs.SelectMany(o => o.Validate()));
            issues.AddRange(m_inputs.SelectMany(o => o.Validate()));
            return issues;
        }

        /// <summary>
        /// Indicates whether the Analytic can be saved.
        /// </summary>
        /// <returns> <see cref="true"/> If the Analytic can be saved</returns>
        public bool isValid() => Validate().Count() == 0;

        private void SettingChange(object sender, PropertyChangedEventArgs args)
        {
            AdapterSettingParameterVM setting = (AdapterSettingParameterVM)sender;
            if (args.PropertyName == nameof(setting.Value))
            {
                if (m_instance != null)
                    m_connectionString = AdapterSettingParameterVM.GetConnectionString(Settings.ToList(), m_instance);
            }
        }

        #endregion

        #region [ Static ]

        private static List<TypeDescription> m_analyticTypes = TypeDescription.LoadDataSourceTypes(FilePath.GetAbsolutePath("").EnsureEnd(Path.DirectorySeparatorChar), typeof(IAnalytic));  

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion

    }
}