// ******************************************************************************************************
// SectionViewModel.tsx - Gbtc
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
using Gemstone.Reflection.MemberInfoExtensions;
using Gemstone.StringExtensions;
using GemstoneCommon;
using GemstoneWPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for a Computational Section 
    /// </summary>
    public class SectionVM: ViewModelBase
    {
        #region [ Members ]
        private TemplateSection m_model;
        private ObservableCollection<AnalyticVM> m_analytics;
        private bool m_removed = false;
        private TemplateVM m_parent;

        private string m_name;
        private int m_order;
        private AnalyticSection m_typeID;
        #endregion

        #region[ Properties ]

        public TemplateVM TemplateVM => m_parent;

        public string Description => GetDescription(m_typeID);
      
        /// <summary>
        /// Title used for Header
        /// </summary>
        public string Title => m_name + " (" + GetTitle(m_typeID) + ")";

        /// <summary>
        /// user generated Name of the <see cref="TemplateSection"/>
        /// </summary>
        public string Name
        {
            get => m_name ?? m_model?.Name ?? "";
            set
            {
                m_name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Title));
            }
        }


        /// <summary>
        /// Indicates if the Section has been Removed.
        /// </summary>
        public bool Removed => m_removed;

        /// <summary>
        /// Command to Add an Analytic
        /// </summary>
        public ICommand AddAnalyticCommand { get; }

        /// <summary>
        /// Command to Remove Section
        /// </summary>
        public ICommand DeleteSectionCommand { get; }

        /// <summary>
        /// Command to Add the current Section
        /// </summary>
        public ICommand AddSectionCommand { get; }

        /// <summary>
        /// Command to move the current section up.
        /// </summary>
        public ICommand MoveUpCommand { get; }

        /// <summary>
        /// Command to move the current section down.
        /// </summary>
        public ICommand MoveDownCommand { get; }

        /// <summary>
        /// The <see cref="AnalyticVM"/> associated with this <see cref="AnalyticSection"/>
        /// </summary>
        public ObservableCollection<AnalyticVM> Analytics => m_analytics;

        public int Order
        {
            get => m_order;
            set
            {
                m_order = value;
                OnPropertyChanged();
            }
        }


      

        /// <summary>
        /// Gets the <see cref="AnalyticSection"/> this Section is of
        /// </summary>
        public AnalyticSection AnalyticSection
        {
            get => m_typeID;
            set
            {
                m_typeID = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Description));
            }
        }

        /// <summary>
        /// The ID of the associated Model.
        /// Note that this will be <see cref="Nullable"/> and is NULL if it's a new Model
        /// </summary>
        public int? ID => m_model?.ID ?? null;

        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Creates a new <see cref="TemplateSection"/> VieModel
        /// </summary>
        /// <param name="section"> The <see cref="TemplateSection"/> associated with this ViewModel</param>
        /// <param name="template">The <see cref="TemplateVM"/> associated with this <see cref="TemplateSection"/> </param>
        public SectionVM(TemplateSection section, TemplateVM template)
        {

            m_parent = template;
            m_model = section;

            if (!(section is null))
            {
                m_name = section.Name;
                m_order = section.Order;
                m_typeID = (AnalyticSection)section.AnalyticTypeID;
            }

            AddAnalyticCommand = new RelayCommand(AddAnalytic, () => true);
            DeleteSectionCommand = new RelayCommand(Removal, () => true);
            AddSectionCommand = new RelayCommand(AddSection, isValid);

            MoveDownCommand = new RelayCommand(() => MoveSection(+1), CanMoveDown);
            MoveUpCommand = new RelayCommand(() => MoveSection(-1), CanMoveUp);

            m_analytics = new ObservableCollection<AnalyticVM>();

            LoadAnalytics();
        }

        /// <summary>
        /// Creates a new <see cref="TemplateSection"/> VieModel
        /// </summary>
        /// <param name="section"> The <see cref="TemplateSection"/> associated with this ViewModel</param>
        /// <param name="template">The <see cref="TemplateVM"/> associated with this <see cref="TemplateSection"/> </param>
        public SectionVM(TemplateVM template): this(null, template)
        {

            string name = "Section 1";
            int i = 1;
            while (template.Sections.Where(d => d.Name == name).Any())
            {
                i++;
                name = "Section " + i.ToString();
            }

            m_name = name;
            if (template.Sections.Count == 0)
                m_order = 1;
            else
                m_order = template.Sections.Max(s => s.Order) + 1;

            m_typeID = AnalyticSection.EventDetection;
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Order));
            OnPropertyChanged(nameof(AnalyticSection));
            OnPropertyChanged(nameof(Description));
        }

        #endregion

        #region [ Methods ]

        public void LoadAnalytics()
        {
            
            m_analytics = new ObservableCollection<AnalyticVM>();

            if (!(m_model is null))
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    m_analytics = new ObservableCollection<AnalyticVM>(new TableOperations<Analytic>(connection)
                        .QueryRecordsWhere("TemplateID = {0} AND SectionID = {1}", m_parent.ID, m_model.ID)
                        .Select(d => new AnalyticVM(d, this)));

            OnPropertyChanged(nameof(Analytics));
        }
        private void AddAnalytic()
        {
            m_analytics.Add(new AnalyticVM(this));
            OnPropertyChanged(nameof(Analytics));
        }

        /// <summary>
        /// This calls the proper functions to add this section to The Template
        /// </summary>
        private void AddSection()
        {
            m_parent.AddSectionCallback(this);
        }

        public void Removal()
        {
            m_removed = true;

            foreach (AnalyticVM a in Analytics)
                a.Removal();

            OnPropertyChanged(nameof(Removed));
        }

        private string GetDescription(AnalyticSection sectionType)
        {
            DescriptionAttribute descriptionAttribute;
            string description;

            if (typeof(AnalyticSection).GetMember(sectionType.ToString()).First().TryGetAttribute(out descriptionAttribute))
                description = descriptionAttribute.Description.ToNonNullNorEmptyString(sectionType.ToString()).Split(":").Last();
            else
                description = sectionType.ToString();

            return description;
        }
        private string GetTitle(AnalyticSection sectionType)
        {
            DescriptionAttribute descriptionAttribute;
            string description;

            if (typeof(AnalyticSection).GetMember(sectionType.ToString()).First().TryGetAttribute(out descriptionAttribute))
                description = descriptionAttribute.Description.ToNonNullNorEmptyString(sectionType.ToString()).Split(":").First();
            else
                description = sectionType.ToString();

            return description;
        }

        /// <summary>
        /// Save All changes.
        /// </summary>
        public void Save()
        {
            
            if (m_removed)
                return;

            if (m_model is null)
            {
                m_model = new TemplateSection()
                {
                    TemplateID = m_parent.ID,
                    Name = m_name,
                    AnalyticTypeID = (int)m_typeID,
                    Order = m_order,
                };

                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                {
                    new TableOperations<TemplateSection>(connection).AddNewRecord(m_model);
                    m_model.ID = connection.ExecuteScalar<int>("select seq from sqlite_sequence where name = {0}", "TemplateSection");
                }

            }
            else
            {
                m_model.Name = m_name;
                m_model.AnalyticTypeID = (int)m_typeID;
                m_model.Order = m_order;
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    new TableOperations<TemplateSection>(connection).UpdateRecord(m_model);
            }

            foreach (AnalyticVM analytic in m_analytics)
                analytic.Save();            
        }

        /// <summary>
        /// Delete any unused models form the Database.
        /// </summary>
        public void Delete()
        {

            foreach (AnalyticVM analytic in m_analytics)
                analytic.Delete();

            if (!m_removed && !m_parent.Removed)
                return;

            if (m_model is null)
                return;

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                new TableOperations<TemplateSection>(connection).DeleteRecord(m_model.ID);           
        }




        /// <summary>
        /// Indicates whether the Section can be moved Up.
        /// </summary>
        /// <returns> <see cref="true"/> If the Section can be moved up</returns>
        public bool CanMoveUp()
        {
            if (m_parent.Sections is null || m_parent.Sections.Count() == 0)
                return false;

            if (m_parent.Sections.First() == this)
                return false;
            return true;
        }

        /// <summary>
        /// Indicates whether the Section can be moved down.
        /// </summary>
        /// <returns> <see cref="true"/> If the Section can be moved down</returns>
        public bool CanMoveDown()
        {
            if (m_parent.Sections is null || m_parent.Sections.Count() == 0)
                return false;

            if (m_parent.Sections.Last() == this)
                return false;
            return true;
        }

        /// <summary>
        /// Moves a Section in the Parent Template
        /// </summary>
        /// <param name="steps"> indicates where to move this section < 0 means up, > 0 means down</param>
        private void MoveSection(int steps)
        {
            int currentIndex = m_parent.Sections.IndexOf(this);
            if (currentIndex == -1)
                return;
            m_parent.SwapSectionOrder(currentIndex, currentIndex + steps);

        }

        /// <summary>
        /// Determines Whether the Section has Changed
        /// </summary>
        /// <returns></returns>
        public bool HasChanged()
        {
            if (m_model == null)
                return !m_removed;

            bool changed = m_model.Name != m_name;
            changed = changed || m_model.Order != m_order;
            changed = changed || m_model.AnalyticTypeID != (int)m_typeID;
            changed = changed || m_analytics.Any(d => d.HasChanged());

            return changed;

        }

        /// <summary>
        /// Lists any Issues preventing this Section from being Saved
        /// </summary>
        /// <returns></returns>
        public List<string> Validate()
        {
            List<string> issues = new List<string>();

            if (m_removed)
                return issues;

            if (string.IsNullOrEmpty(m_name))
                issues.Add($"A name is required for every Section.");

            int nDev = m_parent.Sections.Where(item => item.Name == m_name).Count();
            if (nDev > 1)
                issues.Add($"A Section with name {m_name} already exists.");

            issues.AddRange(m_analytics.SelectMany(d => d.Validate()));
            return issues;
        }

        /// <summary>
        /// Indicates whether the Section can be saved.
        /// </summary>
        /// <returns> <see cref="true"/> If the Section can be saved</returns>
        public bool isValid() => Validate().Count() == 0;

        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        
        #endregion
    }
}