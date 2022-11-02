// ******************************************************************************************************
//  TaskViewModel.tsx - Gbtc
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
//  09/07/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using Adapt.Models;
using Adapt.View.Template;
using Adapt.ViewModels.Common;
using AdaptLogic;
using Gemstone.Data;
using Gemstone.Data.Model;
using Gemstone.IO;
using Gemstone.StringExtensions;
using GemstoneCommon;
using GemstoneWPF;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Transactions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using static Adapt.ViewModels.DeviceMapping;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for Task Window
    /// </summary>
    public class TaskVM : AdaptTabViewModelBase
    {
        #region [ Members ]
        private Template m_SelectedTemplate;
        private DataSource m_SelectedDataSource;
        private AdaptViewModel m_parent;
        #endregion

        #region[ Properties ]

        /// <summary>
        /// Represents a List of all available DataSources.
        /// </summary>
        public ObservableCollection<DataSource> DataSources { get; set; }

        /// <summary>
        /// Represents a List of all available Templates.
        /// </summary>
        public ObservableCollection<Template> Templates { get; set; }

        /// <summary>
        /// The <see cref="DataSource"/> selected.
        /// </summary>
        public DataSource DataSource {
            get => m_SelectedDataSource;
            set 
            {
                m_SelectedDataSource = value;
                ValidateDataSource();
                OnPropertyChanged();
            } 
        }

        public DateSelectVM TimeSelectionViewModel { get; }

        /// <summary>
        /// a Flag indicating if the DataSource has failed the Test. 
        /// This will prevent the User from continuing and display a DataSource Warning Message
        /// </summary>
        public bool ValidatedDataSource { get; private set; }

        /// <summary>
        /// The Template Selected
        /// </summary>
        public Template Template
        {
            get => m_SelectedTemplate;
            set
            {
                m_SelectedTemplate = value;
                ValidateTemplate();
                OnPropertyChanged();                
            }
        }

        /// <summary>
        /// The VMs containing the mapping between Template Devices and DataSource Devices
        /// </summary>
        public ObservableCollection<MappingVM> MappingViewModels { get; set; }

        public ICommand RunTask { get; set; }

        public ICommand AddMapping { get; set; }

        public ICommand AutoMapping { get; set; }

        public bool AllowAutoMapping
        {
            get;
            private set; 
        }

        public IDataSource DataSourceInstance { get; private set; }
        #endregion

        #region [ Constructor ]

        // #ToDo: Add logic to save Task to temporary File to avoid having to set it up every time Adapt/SciSync is opened
        /// <summary>
        /// Creates a new <see cref="TaskVM"/>
        /// </summary>
        /// <param name="ParentVM"> The parent <see cref="AdaptViewModel"/> used to trigger processing of a Task. </param>
        public TaskVM(AdaptViewModel ParentVM)
        {
            m_parent = ParentVM;
            MappingViewModels = new ObservableCollection<MappingVM>();
            LoadDataSources();
            LoadTemplates();
            TimeSelectionViewModel = new DateSelectVM();

           
            ValidateDataSource();
            AddMappingVM();
            AllowAutoMapping = false;

            AddMapping = new RelayCommand(AddMappingVM, () => true);
            AutoMapping = new RelayCommand(GenerateAutoMapping, () => AllowAutoMapping);

            RunTask = new RelayCommand(ProcessTask, () => ValidatedDataSource);
        }

        #endregion

        #region [ Methods ]
        
        /// <summary>
        /// Loads all Templates available.
        /// </summary>
        public void LoadTemplates()
        {
            Templates = new ObservableCollection<Template>();

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                Templates = new ObservableCollection<Template>(
                    new TableOperations<Template>(connection).QueryRecords().ToList()
                    );

            OnPropertyChanged(nameof(Templates));
            Template = Templates.Where(d => d.Id == (Template?.Id ?? -1)).FirstOrDefault();

            if (Template is null && Templates.Count > 0)
                Template = Templates.First();

        }

        /// <summary>
        /// Loads all DataSources available.
        /// </summary>
        public void LoadDataSources()
        {
            DataSources = new ObservableCollection<DataSource>();

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                DataSources = new ObservableCollection<DataSource>(
                    new TableOperations<DataSource>(connection).QueryRecords().ToList()
                    );
            
            OnPropertyChanged(nameof(DataSources));
            DataSource = DataSources.Where(d => d.ID == (DataSource?.ID ?? -1)).FirstOrDefault();

            if (DataSource is null && DataSources.Count > 0)
                DataSource = DataSources.First();
        }

        private void ValidateDataSource()
        {
            
            if (DataSource is null)
            {
                ValidatedDataSource = false;
                OnPropertyChanged(nameof(ValidatedDataSource));
                return;
            }

            try
            {
                DataSourceInstance = (IDataSource)Activator.CreateInstance(DataSource.AssemblyName, DataSource.TypeName).Unwrap();
                IConfiguration config = new ConfigurationBuilder().AddGemstoneConnectionString(DataSource.ConnectionString).Build();
                DataSourceInstance.Configure(config);

                ValidatedDataSource = DataSourceInstance.Test();

                foreach (MappingVM mapping in MappingViewModels)
                    mapping.UpdateDataSource(DataSource);

                OnPropertyChanged(nameof(MappingViewModels));
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Datasource {DataSource.ID} Test Failed Exception: {ex.Message} StackTrace: {ex.StackTrace}");
                ValidatedDataSource = false;
            }
            finally
            {
                OnPropertyChanged(nameof(ValidatedDataSource));
            }
        }

        private void ValidateTemplate()
        {
            MappingViewModels = new ObservableCollection<MappingVM>();
            AddMappingVM();

            int NDevices = 0;

            if (!(Template is null))
                using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
                    NDevices = new TableOperations<TemplateInputDevice>(connection).QueryRecordCountWhere("TemplateID = {0} AND (SELECT COUNT(ID) FROM TemplateInputSignal WHERE DeviceID = TemplateInputDevice.ID) > 0", Template.Id);

            AllowAutoMapping = NDevices == 1;
            
            OnPropertyChanged(nameof(MappingViewModels));
            OnPropertyChanged(nameof(AllowAutoMapping));
        }

        private void AddMappingVM()
        {
            if (Template is null)
                return;

            MappingViewModels.Add(new MappingVM(Template, this));
            ValidateDataSource();
        }

        public void RemoveMapping(MappingVM vm)
        {
            MappingViewModels.Remove(vm);
        }

        private void ProcessTask()
        {
            AdaptTask task = new AdaptTask()
            {
                DataSourceModel = DataSource,
                TemplateModel = Template,
                Start = TimeSelectionViewModel.Start,
                End = TimeSelectionViewModel.End
            };

            //Load Information from Database
            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString,DataProviderString))
            {
                TableOperations<Analytic> analyticTbl = new TableOperations<Analytic>(connection);
                TableOperations<AnalyticInput> inputTbl = new TableOperations<AnalyticInput>(connection);
                TableOperations<AnalyticOutputSignal> outputTbl = new TableOperations<AnalyticOutputSignal>(connection);


                task.Sections = new TableOperations<TemplateSection>(connection).QueryRecordsWhere("TemplateID = {0}", Template.Id).OrderBy(s => s.Order).Select(s => new AdaptTask.TaskSection() { 
                    Model = s,
                    Analytics = analyticTbl.QueryRecordsWhere("SectionID = {0}",s.ID).Select(analytic => {
                        IConfiguration config = new ConfigurationBuilder().AddGemstoneConnectionString(analytic.ConnectionString).Build();
                        Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(analytic.AssemblyName));
                        Type type = assembly.GetType(analytic.TypeName);

                        return new AdaptTask.TaskAnalytic()
                        {
                            Model = analytic,
                            InputModel = inputTbl.QueryRecordsWhere("AnalyticID = {0}", analytic.ID).ToList(),
                            OutputModel = outputTbl.QueryRecordsWhere("AnalyticID = {0}", analytic.ID).ToList(),
                            AnalyticType = type,
                            Configuration = config,
                        };
                    }).ToList()
                }).ToList();

                task.DevicesModels = new TableOperations<TemplateInputDevice>(connection).QueryRecordsWhere("TemplateID={0}", Template.Id).ToList();
                task.OutputSignalModels = new TableOperations<TemplateOutputSignal>(connection).QueryRecordsWhere("TemplateID={0}", Template.Id).ToList();
                if (task.DevicesModels.Count > 0)
                    task.SignalModels = new TableOperations<TemplateInputSignal>(connection).QueryRecordsWhere($"DeviceID IN ({string.Join(",", task.DevicesModels.Select(d => d.ID))})", Template.Id).ToList();
                else
                    task.SignalModels = new List<TemplateInputSignal>();
            }

            task.DeviceMappings = MappingViewModels.Select(item => item.DeviceMap).ToList();
            task.SignalMappings = MappingViewModels.Select(item => item.SignalMap).ToList();

            m_parent.ProcessTask(task);
        }

        private void GenerateAutoMapping()
        {
            ValidateDataSource();


            // Start by opening the Device List to select Devcies....
            SelectSignal selectionWindow = new SelectSignal();

            SelectMappingVM<AdaptDevice> deviceSelectionVM = new SelectMappingVM<AdaptDevice>((devices) => {

                MappingViewModels = new ObservableCollection<MappingVM>();

                foreach (AdaptDevice d in devices)
                {
                    MappingVM mapping = new MappingVM(Template, this);
                    mapping.UpdateDataSource(DataSource);
                    mapping.AssignDevice(d);
                    MappingViewModels.Add(mapping);
                }
                selectionWindow.Close();
                OnPropertyChanged(nameof(MappingViewModels));
            }, (d, s) => d.Name.ToLower().Contains(s.ToLower()), (d) => d.Name, AdaptDevice.Get(DataSourceInstance, DataSource.ID, ConnectionString, DataProviderString),"Select Devices To Include",true); ;
            selectionWindow.DataContext = deviceSelectionVM;
            selectionWindow.ShowDialog();
        }
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        
        #endregion
    }
}