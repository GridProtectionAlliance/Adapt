// ******************************************************************************************************
//  NewTemplateWindowViewModel.tsx - Gbtc
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
//  03/20/2022 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using Adapt.Models;
using Adapt.View;
using Adapt.ViewModels.Common;
using Gemstone.Data;
using Gemstone.Data.Model;
using Gemstone.IO;
using Gemstone.StringExtensions;
using GemstoneCommon;
using GemstoneWPF;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for New DataSource Window
    /// </summary>
    public class NewTemplateWindowVM: ViewModelBase
    {
        #region [ Members ]

        private Template m_template;
        private RelayCommand m_saveCommand;

        #endregion

        #region[ Properties ]

        public Template Template
        {
            get { return m_template; }  
        }

        public string Name
        {
            get => m_template?.Name ?? "";
            set
            {
                m_template.Name = value;
                OnPropertyChanged();
            }
        }



        /// <summary>
        /// Event that get's raised when a new Template has been saved
        /// </summary>
        public event EventHandler<TemplateAddedArgs> AddedTemplate;

        public ICommand SaveCommand => m_saveCommand;
        public bool CanSave => ValidConfig();

        #endregion

        #region [ Constructor ]
        
        public NewTemplateWindowVM()
        {
            m_template = new Template() 
            { 
                Id=-1,
                Name="",
            };

            m_saveCommand = new RelayCommand(Save, () => CanSave);
         
        }

        #endregion

        #region [ Methods ]
        public void Save()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                              int id = 0;
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                {
                    new TableOperations<Template>(connection).AddNewRecord(m_template);
                    id = connection.ExecuteScalar<int>("Select ID FROM Template WHERE Name = {0}", m_template.Name);
                }
                m_template.Id = id;
                AddedTemplate?.Invoke(this, new TemplateAddedArgs(id, m_template));

            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    Popup(ex.Message + Environment.NewLine + "Inner Exception: " + ex.InnerException.Message, "Add Data Source Exception:", MessageBoxImage.Error);
                }
                else
                {
                    Popup(ex.Message, "Add Data Source Exception:", MessageBoxImage.Error);
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }


        private bool ValidConfig()
        {
            if (m_template.Name == null || m_template.Name.Length == 0)
                return false;

            return true;
        }

        #endregion

        #region [ Static ]
        
        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion
    }
}