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
        private TemplateSection m_section;
        private ObservableCollection<AnalyticVM> m_analytics;
        #endregion

        #region[ Properties ]

        public string Description => GetDescription((AnalyticSection)m_section.AnalyticTypeID);
      
        /// <summary>
        /// Title used for Header
        /// </summary>
        public string Title => m_section.Name + " (" + GetTitle((AnalyticSection)m_section.AnalyticTypeID) + ")";

        /// <summary>
        /// Indicates if the Section has changed.
        /// </summary>
        public bool Changed => true;

        /// <summary>
        /// Command to Add an Analytic
        /// </summary>
        public ICommand AddAnalyticCommand { get; }

        /// <summary>
        /// The <see cref="AnalyticVM"/> associated with this <see cref="AnalyticSection"/>
        /// </summary>
        public ObservableCollection<AnalyticVM> Analytics => m_analytics;

        public int Order => m_section.Order;

        /// <summary>
        /// The associated Template ViewModel
        /// </summary>
        public TemplateVM TemplateViewModel { get; set; }
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Creates a new <see cref="TemplateSection"/> VieModel
        /// </summary>
        /// <param name="section"> The <see cref="TemplateSection"/> associated with this ViewModel</param>
        /// <param name="template">The <see cref="TemplateVM"/> associated with this <see cref="TemplateSection"/> </param>
        public SectionVM(TemplateSection section, TemplateVM template)
        {
            TemplateViewModel = template;
            m_section = section;

            AddAnalyticCommand = new RelayCommand(AddAnalytic, () => true);
            m_analytics = new ObservableCollection<AnalyticVM>();
        }

        #endregion

        #region [ Methods ]

        public void LoadAnalytics()
        {
            m_analytics = new ObservableCollection<AnalyticVM>();
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                m_analytics = new ObservableCollection<AnalyticVM>(new TableOperations<Analytic>(connection)
                    .QueryRecordsWhere("TemplateID = {0} AND SectionID = {1}", TemplateViewModel.ID, m_section.ID)
                    .Select(d => new AnalyticVM(d, this)));

            m_analytics.ToList().ForEach(d => d.LoadOutputs());
            m_analytics.ToList().ForEach(d => d.LoadInputs());

            OnPropertyChanged(nameof(Analytics));
        }
        private void AddAnalytic()
        {
            m_analytics.Add(new AnalyticVM(new Analytic() { 
                Name="Test name",
                SectionID = m_section.ID,
                TemplateID = TemplateViewModel.ID,
                ID = TemplateViewModel.CreateAnalyticID()
            }, this));

            OnPropertyChanged(nameof(Analytics));
        }
        private string GetDescription(AnalyticSection sectionType)
        {
            DescriptionAttribute descriptionAttribute;
            string description;

            if (typeof(AnalyticSection).GetMember(sectionType.ToString()).First().TryGetAttribute(out descriptionAttribute))
                description = descriptionAttribute.Description.ToNonNullNorEmptyString(m_section.ToString()).Split(":").Last();
            else
                description = m_section.ToString();

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
            if (!Changed)
                return;

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            {
                TableOperations<TemplateSection> templateTbl = new TableOperations<TemplateSection>(connection);
                if (m_section.ID < 0)
                {
                    int templateId = new TableOperations<Template>(connection).QueryRecordWhere("Name = {0}", TemplateViewModel.Name).Id;
                    templateTbl.AddNewRecord(new TemplateSection()
                    {
                        Name = m_section.Name,
                        AnalyticTypeID = m_section.AnalyticTypeID,
                        Order = m_section.Order,
                        TemplateID = templateId
                    });
                }
                else
                    templateTbl.UpdateRecord(m_section);

                m_analytics.ToList().ForEach(a => a.Save());
            }
        }
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        
        #endregion
    }
}