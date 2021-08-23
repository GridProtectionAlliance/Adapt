﻿// ******************************************************************************************************
// NewSectionViewModel.tsx - Gbtc
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
//  08/06/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using Adapt.Models;
using Gemstone.IO;
using GemstoneWPF;
using System;
using System.Collections.Generic;
using System.IO;
namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for a Computational Section 
    /// </summary>
    public class NewSectionVM: ViewModelBase
    {
        #region [ Classes ]
        public class SectionTypeDescriptionVM: ViewModelBase
        {
            public AnalyticSection Type { get; }
            public string Name { get; }
            public string Description { get; }
            public SectionTypeDescriptionVM(AnalyticSection section)
            {
                Type = section;
                Name = section.ToString();
                Description = "Test Description";
            }
        }
        #endregion

        #region [ Members ]

        private string m_Name;
        private int m_selectedType;
        private Action<TemplateSection> m_complete;
        #endregion

        #region[ Properties ]

        public string Name
        {
            get => m_Name;
            set 
            {
                m_Name = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand AddSection { get; }

        public int TypeSelectedIndex
        {
            get => m_selectedType;
            set { m_selectedType = value; OnPropertyChanged(); }
        }

        public List<SectionTypeDescriptionVM> TypeOptions { get; }
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Viewmodel for Creating a new <see cref="TemplateSection"/>
        /// </summary>
        /// <param name="onComplete"> The Action do be done once the new <see cref="TemplateSection"/> is created </param>
        public NewSectionVM(Action<TemplateSection> onComplete)
        {
            m_Name = "Section";
            m_selectedType = 0;
            m_complete = onComplete;
            TypeOptions = new List<SectionTypeDescriptionVM>() {
                new SectionTypeDescriptionVM(AnalyticSection.DataCleanup),
                new SectionTypeDescriptionVM(AnalyticSection.DataFiltering),
                new SectionTypeDescriptionVM(AnalyticSection.SignalProcessing),
                new SectionTypeDescriptionVM(AnalyticSection.EventDetection),
            };

            AddSection = new RelayCommand(CreateSection, () => true);
        }

        #endregion

        #region [ Methods ]

        private void CreateSection()
        {
            TemplateSection section = new TemplateSection()
            {
                Name = m_Name,
                AnalyticTypeID = (int)TypeOptions[m_selectedType].Type,
            };
            m_complete.Invoke(section);
        }
       
        /*private string GetDescription()
        {
            DescriptionAttribute descriptionAttribute;
            string description;

            if (typeof(AnalyticSection).GetMember(m_section.ToString()).First().TryGetAttribute(out descriptionAttribute))
                description = descriptionAttribute.Description.ToNonNullNorEmptyString(m_section.ToString());
            else
                description = m_section.ToString();

            return description;
        }*/
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        
        #endregion
    }
}