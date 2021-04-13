﻿// ******************************************************************************************************
//  DeviceViewModel.tsx - Gbtc
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
//  04/01/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using Adapt.Models;
using Gemstone.Data;
using Gemstone.Data.Model;
using Gemstone.IO;
using GemstoneWPF;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for PMU MetaData
    /// </summary>
    public class DeviceViewModel: ViewModelBase
    {
        #region [ Members ]

        private AdaptDevice m_device;
        private IEnumerable<SignalViewModel> m_signals;
        private string m_Name;
        private int m_dataSourceID;
        #endregion

        #region[ Properties ]

        public string Name
        {
            get => m_Name;
            set
            {
                m_Name = value;
                SaveCustomName();
                OnPropertyChanged();
            }
        }

        public int NSignals => m_signals.Count();

        /// <summary>
        /// Gets the List of <see cref="SignalViewModel"/> associated with this Device.
        /// </summary>
        public IEnumerable<SignalViewModel> Signals => m_signals;
        #endregion

        #region [ Constructor ]
        public DeviceViewModel(AdaptDevice device, IEnumerable<AdaptSignal> signals, int DataSourceID)
        {
            m_device = device;
            m_signals = signals.Select(s => new SignalViewModel(s,DataSourceID));
            m_Name = GetCustomName();
            m_dataSourceID = DataSourceID;
        }

        #endregion

        #region [ Methods ]

        /// Checks the Database for a custom Name. If non is available it will return the Phase provided by the <see cref="IDataSource"/>.
        /// </summary>
        /// <returns>The <see cref="Name"/> associated with this Device</returns>
        private string GetCustomName()
        {
            bool hasCustom = false;
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            {
                hasCustom = connection.ExecuteScalar<bool>($"SELECT (COUNT(ID) > 0) FROM DeviceMetaData WHERE DataSourceID={m_dataSourceID} AND DeviceID='{m_device.ID}' AND Field='Name' ");

                if (hasCustom)
                    return connection.ExecuteScalar<string>($"SELECT Value FROM DeviceMetaData WHERE DataSourceID={m_dataSourceID} AND DeviceID='{m_device.ID}' AND Field='Name' ");
            }
            return m_device.Name;
        }

        /// <summary>
        /// Checks if the Name is Different from the Name returned by the <see cref="IDataSource"/> and saves it to the Database if necessary.
        /// </summary>
        private void SaveCustomName()
        {
            bool hasCustom = false;
            bool isCustom = (object)m_Name != null && m_Name != m_device.Name;

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            {
                hasCustom = connection.ExecuteScalar<bool>($"SELECT (COUNT(ID) > 0) FROM DeviceMetaData WHERE DataSourceID={m_dataSourceID} AND DeviceID='{m_device.ID}' AND Field='Name' ");

                if (isCustom && !hasCustom)
                    connection.ExecuteNonQuery($"INSERT INTO DeviceMetaData (DataSourceID,DeviceID,Field,Value) VALUES ({m_dataSourceID},'{m_device.ID}','','Name','{m_Name}')");
                if (isCustom && hasCustom)
                    connection.ExecuteNonQuery($"UPDATE DeviceMetaData SET Value = '{m_Name}' WHERE DataSourceID={m_dataSourceID} AND DeviceID='{m_device.ID}' AND Field='Name'");
                if (!isCustom && hasCustom)
                    connection.ExecuteNonQuery($"DELETE DeviceMetaData WHERE DataSourceID={m_dataSourceID} AND DeviceID='{m_device.ID}' AND Field='Name' ");
            }
        }

        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion
    }
}