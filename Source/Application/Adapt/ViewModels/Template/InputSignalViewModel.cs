﻿// ******************************************************************************************************
// InputSignalViewModel.tsx - Gbtc
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
using GemstoneCommon;
using GemstoneWPF;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for Template Input Signal
    /// </summary>
    public class InputSignalVM: ViewModelBase
    {
        #region [ Members ]

        
        private int m_templateID;
        #endregion

        #region[ Properties ]

        public string Name
        {
            get => "";
            set
            {
                //m_device.Name = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Creates a new <see cref="TemplateInputDevice"/> VieModel
        /// </summary>
        /// <param name="device"> The <see cref="TemplateInputDevice"/> associated with this ViewModel</param>
        /// <param name="DataSourceID">The ID of the <see cref="Template"/> </param>
        public InputSignalVM()
        {
        }

        #endregion

        #region [ Methods ]

        public void Save()
        {
            //using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            //    new TableOperations<TemplateInputDevice>(connection).AddNewOrUpdateRecord(m_device);

        }

        public void Delete()
        {
            //using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            //    new TableOperations<TemplateInputDevice>(connection).DeleteRecord(m_device);

        }


        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion
    }
}