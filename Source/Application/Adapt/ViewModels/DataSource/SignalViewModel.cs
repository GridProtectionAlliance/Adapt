// ******************************************************************************************************
//  SignalViewModel.tsx - Gbtc
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
using Adapt.View.Common;
using Adapt.View.Visualization;
using Adapt.ViewModels.Common;
using Adapt.ViewModels.Vizsalization;
using AdaptLogic;
using Gemstone.Data;
using Gemstone.Data.Model;
using Gemstone.IO;
using GemstoneCommon;
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
    /// ViewModel for Signal MetaData
    /// </summary>
    public class SignalViewModel: ViewModelBase
    {
        #region [ Members ]

        private AdaptSignal m_signal;
        private int m_dataSourceID;
        private Phase m_phase;
        private MeasurementType m_type;
        private string m_Name;
        private RelayCommand m_VisualizeCommand;

        #endregion

        #region[ Properties ]

        /// <summary>
        /// Gets the Command to open the Visualization for this <see cref="AdaptSignal"/>
        /// </summary>
        public ICommand Visualize =>  m_VisualizeCommand;

        /// <summary>
        /// Gets or Sets the <see cref="Phase"/> associated with this <see cref="AdaptSignal"/>
        /// </summary>
        public Phase Phase
        {
            get => m_phase;
            set
            {
                m_phase = value;
                SaveCustomPhase();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or Sets the <see cref="AdaptMeasurementType"/> associated with this <see cref="AdaptSignal"/>
        /// </summary>
        public MeasurementType Type
        {
            get => m_type;
            set
            {
                m_type = value;
                SaveCustomType();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or Sets the Name associated with this <see cref="AdaptSignal"/>
        /// </summary>
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
        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates a new <see cref="SignalViewModel"/>
        /// </summary>
        /// <param name="Signal"> The <see cref="AdaptSignal"/> this View  Model is based on.</param>
        /// <param name="DataSourceID">The ID of the <see cref="DataSource"/> used to identify Signals in the Database</param>
        /// <param name="CustomTypes"> A Dictionary of Custom Measurement Types to avoid overloading the SQLite DB with calls.</param>
        /// <param name="CustomPhases"> A Dictionary of Custom Phases to avoid overloading the SQLite DB with calls.</param>
        /// <param name="CustomNames"> A Dictionary of Custom Names to avoid overloading the SQLite DB with calls.</param>
        public SignalViewModel(AdaptSignal Signal, int DataSourceID, Dictionary<string,MeasurementType> CustomTypes, Dictionary<string, Phase> CustomPhases, Dictionary<string, string> CustomNames)
        {
            m_signal = Signal;
            m_dataSourceID = DataSourceID;

            m_phase = GetCustomPhase(CustomPhases);
            m_type = GetCustomType(CustomTypes);
            m_Name = GetCustomName(CustomNames);

            m_VisualizeCommand = new RelayCommand(OpenVisualize, () => true);
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Checks the Database for a custom Phase. If non is available it will return the Phase provided by the <see cref="IDataSource"/>.
        /// </summary>
        /// <param name="CustomPhases"> A Dictionary to get custom Phases</param>
        /// <returns>The <see cref="Phase"/> associated with this Signal</returns>
        private Phase GetCustomPhase(Dictionary<string, Phase> CustomPhases)
        {
            bool hasCustom = CustomPhases.ContainsKey(m_signal.ID);

            if (hasCustom)
                return CustomPhases[m_signal.ID];
             
            return m_signal.Phase;
        }

        /// <summary>
        /// Checks if the Phase is Different from the <see cref="Phase"/> returned by the <see cref="IDataSource"/> and saves it to the Database if necessary.
        /// </summary>
        private void SaveCustomPhase()
        {
            bool hasCustom = false;
            bool isCustom = (object)m_phase != null && m_phase != m_signal.Phase;

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            {
                hasCustom = connection.ExecuteScalar<bool>($"SELECT (COUNT(ID) > 0) FROM SignalMetaData WHERE DataSourceID={m_dataSourceID} AND SignalID='{m_signal.ID}' AND Field='Phase' ");

                if (isCustom && !hasCustom)
                    connection.ExecuteNonQuery($"INSERT INTO SignalMetaData (DataSourceID,SignalID,DeviceID,Field,Value) VALUES ({m_dataSourceID},'{m_signal.ID}','','Phase','{m_phase}')");
                if (isCustom && hasCustom)
                    connection.ExecuteNonQuery($"UPDATE SignalMetaData SET Value = '{m_phase}' WHERE DataSourceID={m_dataSourceID} AND SignalID='{m_signal.ID}' AND Field='Phase'");
                if (!isCustom && hasCustom)
                    connection.ExecuteNonQuery($"DELETE SignalMetaData WHERE DataSourceID={m_dataSourceID} AND SignalID='{m_signal.ID}' AND Field='Phase' ");
            }
        }

        /// <summary>
        /// Checks the Database for a custom MeasurementType. If non is available it will return the Phase provided by the <see cref="IDataSource"/>.
        /// </summary>
        /// <param name="CustomTypes"> A Dictionary to look up any custom Measurement Types. </param>
        /// <returns>The <see cref="AdaptMeasurementType"/> associated with this Signal</returns>
        private MeasurementType GetCustomType(Dictionary<string, MeasurementType> CustomTypes)
        {
            bool hasCustom = CustomTypes.ContainsKey(m_signal.ID);

            if (hasCustom)
                return CustomTypes[m_signal.ID];

            return m_signal.Type;
        }

        /// <summary>
        /// Checks if the Type is Different from the <see cref="AdaptMeasurementType"/> returned by the <see cref="IDataSource"/> and saves it to the Database if necessary.
        /// </summary>
        private void SaveCustomType()
        {
            bool hasCustom = false;
            bool isCustom = (object)m_type != null && m_type != m_signal.Type;

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            {
                hasCustom = connection.ExecuteScalar<bool>($"SELECT (COUNT(ID) > 0) FROM SignalMetaData WHERE DataSourceID={m_dataSourceID} AND SignalID='{m_signal.ID}' AND Field='Type' ");

                if (isCustom && !hasCustom)
                    connection.ExecuteNonQuery($"INSERT INTO SignalMetaData (DataSourceID,SignalID,DeviceID,Field,Value) VALUES ({m_dataSourceID},'{m_signal.ID}','','Type','{m_type}')");
                if (isCustom && hasCustom)
                    connection.ExecuteNonQuery($"UPDATE SignalMetaData SET Value = '{m_type}' WHERE DataSourceID={m_dataSourceID} AND SignalID='{m_signal.ID}' AND Field='Type'");
                if (!isCustom && hasCustom)
                    connection.ExecuteNonQuery($"DELETE SignalMetaData WHERE DataSourceID={m_dataSourceID} AND SignalID='{m_signal.ID}' AND Field='Type' ");
            }
        }

        /// <summary>
        /// Checks the Database for a custom Name. If non is available it will return the Phase provided by the <see cref="IDataSource"/>.
        /// </summary>
        /// <param name="CustomNames"> A Dictionary to look up Custom Names</param>
        /// <returns>The <see cref="Name"/> associated with this Signal</returns>
        private string GetCustomName(Dictionary<string, string> CustomNames)
        {
            bool hasCustom = CustomNames.ContainsKey(m_signal.ID);

            if (hasCustom)
                return CustomNames[m_signal.ID];
              
            return m_signal.Name;
        }

        /// <summary>
        /// Checks if the Name is Different from the Name returned by the <see cref="IDataSource"/> and saves it to the Database if necessary.
        /// </summary>
        private void SaveCustomName()
        {
            bool hasCustom = false;
            bool isCustom = (object)m_Name != null && m_Name != m_signal.Name;

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            {
                hasCustom = connection.ExecuteScalar<bool>($"SELECT (COUNT(ID) > 0) FROM SignalMetaData WHERE DataSourceID={m_dataSourceID} AND SignalID='{m_signal.ID}' AND Field='Name' ");

                if (isCustom && !hasCustom)
                    connection.ExecuteNonQuery($"INSERT INTO SignalMetaData (DataSourceID,SignalID,DeviceID,Field,Value) VALUES ({m_dataSourceID},'{m_signal.ID}','','Name','{m_Name}')");
                if (isCustom && hasCustom)
                    connection.ExecuteNonQuery($"UPDATE SignalMetaData SET Value = '{m_Name}' WHERE DataSourceID={m_dataSourceID} AND SignalID='{m_signal.ID}' AND Field='Name'");
                if (!isCustom && hasCustom)
                    connection.ExecuteNonQuery($"DELETE SignalMetaData WHERE DataSourceID={m_dataSourceID} AND SignalID='{m_signal.ID}' AND Field='Name' ");
            }
        }

        /// <summary>
        /// Opens the Visualization Window for this <see cref="AdaptSignal"/>
        /// </summary>
        public void OpenVisualize()
        {

            DateSelectWindow dateSelection = new DateSelectWindow();
            DateSelectindowVM dateSelectionVM = new DateSelectindowVM(GetData);
            dateSelection.DataContext = dateSelectionVM;
            dateSelection.Show();
        }

        private void GetData(DateTime start, DateTime end)
        {
            DataSource ds;
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                ds = new TableOperations<DataSource>(connection).QueryRecordWhere("ID = {0}", m_dataSourceID);

            TaskProcessor taskProcessor = new TaskProcessor(new List<AdaptSignal>() { m_signal }, ds, start, end);
            
            ProcessNotificationWindow progress = new ProcessNotificationWindow();
            ProcessNotificationWindowVM progressVM = new ProcessNotificationWindowVM();
            progress.DataContext = progressVM;
            progress.Show();

            taskProcessor.ReportProgress += (object e, ProgressArgs arg) => {  
                if(arg.Complete)
                {
                    progress.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(() =>
                    {
                        progress.Close();
                        VisualizationWindow visWindow = new VisualizationWindow();
                        visWindow.DataContext = new VisualizationWindowVM(start, end);

                        visWindow.Show();
                    }));
                }
                else
                    progressVM.Update(arg); 
            };

            taskProcessor.StartTask();


        }


        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion
    }
}