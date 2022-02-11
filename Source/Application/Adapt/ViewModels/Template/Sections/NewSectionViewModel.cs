// ******************************************************************************************************
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
using System.Linq;

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
                Name = GemstoneCommon.EnumDescriptionHelper.GetDisplayName(section);
                Description = GemstoneCommon.EnumDescriptionHelper.GetEnumDescription(section);
            }
        }
        #endregion

        #region [ Members ]

        private string m_Name;
        private int m_selectedType;
        private Action<TemplateSection> m_complete;
        private TemplateVM m_templateViemodel;
        private bool m_valid;
        #endregion

        #region[ Properties ]

        public string Name
        {
            get => m_Name;
            set 
            {
                if (m_templateViemodel.Sections.Select(item => item.Name).Contains(value))
                    m_valid = false;
                else
                    m_valid = true;
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
        /// <param name="templateViemodel"> The Viemodel of the full <see cref="Template"/></param>
        public NewSectionVM(Action<TemplateSection> onComplete, TemplateVM templateViemodel)
        {
            m_valid = true;
            m_Name = "Section";
            int i = 1;
            while (templateViemodel.Sections.Select(item => item.Name).Contains(m_Name))
            {
                m_Name = "Section " + i.ToString();
                i++;
            }

            m_templateViemodel = templateViemodel;
            m_selectedType = 0;
            m_complete = onComplete;
            TypeOptions = new List<SectionTypeDescriptionVM>() {
                new SectionTypeDescriptionVM(AnalyticSection.DataCleanup),
                new SectionTypeDescriptionVM(AnalyticSection.DataFiltering),
                new SectionTypeDescriptionVM(AnalyticSection.SignalProcessing),
                new SectionTypeDescriptionVM(AnalyticSection.EventDetection),
            };

            AddSection = new RelayCommand(CreateSection, () => m_valid);
        }

        #endregion

        #region [ Methods ]

        private void CreateSection()
        {
            if (m_templateViemodel.Sections.Where(s => s.Name.ToLower() == m_Name.ToLower()).Count() > 1)
            {
                Popup("Section Already Exists", "A Section with this Name already exists. Please choose a unique name.", System.Windows.MessageBoxImage.Error);
            return;
            }

            TemplateSection section = new TemplateSection()
            {
                Name = m_Name,
                AnalyticTypeID = (int)TypeOptions[m_selectedType].Type,
            };
            m_complete.Invoke(section);
        }
       
        
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        
        #endregion
    }
}