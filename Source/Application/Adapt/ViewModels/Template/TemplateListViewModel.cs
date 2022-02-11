// ******************************************************************************************************
//  TemplateListViewModel.tsx - Gbtc
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
//  05/28/2020 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using Adapt.Models;
using Adapt.View;
using Gemstone.Data;
using Gemstone.Data.Model;
using Gemstone.IO;
using GemstoneWPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for Template List Window
    /// </summary>
    public class TemplateListVM : ViewModelBase
    {
        #region [ Members ]

        private List<Template> m_templates;
        private int m_selectedIndex;
        #endregion

        #region[ Properties ]

        public List<Template> Templates
        {
            get { return m_templates; }
            set
            {
                m_templates = value;
                OnPropertyChanged();
            }
        }

        public int SelectedID => (m_selectedIndex > -1? m_templates[m_selectedIndex].Id : -1);
        
        public int SelectedIndex
        {
            get => m_selectedIndex;
            set
            {
                if (m_selectedIndex == value)
                    return;
                m_selectedIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedID));
            }
        }

        public ICommand AddNewCommand { get; set; }

        #endregion

        #region [ Constructor ]
        public TemplateListVM()
        {
            AddNewCommand = new RelayCommand(AddNewTemplate, () => true);
            m_selectedIndex = -1;
            Load();
        }

        #endregion

        #region [ Methods ]

        public void Load(int Id=-1)
        {
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                m_templates = new TableOperations<Template>(connection).QueryRecords().ToList();

            if (m_templates.Count > 0 && m_selectedIndex == -1 && Id == -1)
                SelectedIndex = 0;
            else if (Id != -1)
                SelectedIndex = m_templates.FindIndex(ds => ds.Id == Id);
            else
                SelectedIndex = -1;

            OnPropertyChanged(nameof(Templates));
        }

        public void AddNewTemplate()
        {
           
        }
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion
    }
}