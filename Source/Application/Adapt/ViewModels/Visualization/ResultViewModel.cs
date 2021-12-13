// ******************************************************************************************************
//  ResultViewModel.tsx - Gbtc
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
//  10/06/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using Adapt.Models;
using Adapt.ViewModels.Common;
using Adapt.ViewModels.Visualization.Widgets;
using Adapt.ViewModels.Vizsalization;
using AdaptLogic;
using Gemstone.Data;
using Gemstone.Data.Model;
using Gemstone.IO;
using Gemstone.Reflection.MemberInfoExtensions;
using Gemstone.StringExtensions;
using Gemstone.TypeExtensions;
using GemstoneCommon;
using GemstoneWPF;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Adapt.ViewModels
{
    /// <summary>
    /// Primary ViewModel for <see cref="Adapt.View.Visualization.ResultView"/>
    /// </summary>
    public class ResultViewVM : AdaptTabViewModelBase
    {
        #region [ Members ]
        private ResultState m_resultStatus;
        private ProcessNotificationVM m_progress;
        private TaskProcessor m_processor;
        private MainVisualizationVM m_viewer;
        #endregion

        #region[ Properties ]

        /// <summary>
        /// State that determines which View to show.
        /// </summary>
        public ResultState ResultStatus
        {
            get => m_resultStatus;
            set
            {
                m_resultStatus = value;
                OnPropertyChanged();
            }
        }

        public ProcessNotificationVM ProgressVM => m_progress;
        public MainVisualizationVM VisualizationVM => m_viewer;

        #endregion

        #region[ Constructor]
        public ResultViewVM()
        {
            m_resultStatus = ResultState.NoTask;
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Starts Processing of a Task and shows the Process Bar.
        /// </summary>
        public async void ProcessTask(TaskVM Task)
        {
            m_processor = new TaskProcessor(GenerateTask(Task));

            ResultStatus = ResultState.Processing;
            m_progress = new ProcessNotificationVM();
            m_viewer = new MainVisualizationVM(DateTime.Now, DateTime.Now.AddSeconds(-1));

            m_processor.ReportProgress += (object e, ProgressArgs arg) => {
                if (arg.Complete)
                {
                    ResultStatus = ResultState.View;
                    OnPropertyChanged(nameof(VisualizationVM));
                }
                else
                    m_progress.Update(arg);
            };

            OnPropertyChanged(nameof(ProgressVM));
            await m_processor.StartTask();
            m_viewer = new MainVisualizationVM(Task.TimeSelectionViewModel.Start, Task.TimeSelectionViewModel.End);
        }

        private AdaptTask GenerateTask(TaskVM viewModel)
        {
            AdaptTask result = new AdaptTask();
            result.DataSource = viewModel.DataSources[viewModel.SelectedDataSourceIndex];
            result.Start = viewModel.TimeSelectionViewModel.Start;
            result.End = viewModel.TimeSelectionViewModel.End;
            result.Sections = new List<TaskSection>();

            Dictionary<int, string> tempSignals = new Dictionary<int, string>();
            Dictionary<int, string> inputSignals = new Dictionary<int, string>();

            inputSignals = viewModel.MappingViewModel.DeviceMappings.SelectMany(m => m.ChannelMappings).ToDictionary(x=>x.Key, x=> x.Value);

            Template template = viewModel.Templates[viewModel.SelectedTemplateIndex];

            using (AdoDataConnection connection = new AdoDataConnection(ConnectionString, DataProviderString))
            {
                List<TemplateInputDevice> devices = new TableOperations<TemplateInputDevice>(connection)
                    .QueryRecordsWhere("TemplateID={0}", template.Id).ToList();

                List<TemplateSection> sections = new TableOperations<TemplateSection>(connection)
                    .QueryRecordsWhere("TemplateID={0}", template.Id).OrderBy(item => item.Order).ToList();

                foreach (TemplateSection section in sections)
                {
                    List<Models.Analytic> analytics = (new TableOperations<Models.Analytic>(connection))
                        .QueryRecordsWhere("TemplateID={0} AND SectionID={1}", template.Id, section.ID).ToList();
                    result.Sections.Add(new TaskSection() { Analytics = new List<AdaptLogic.Analytic>() });
                    foreach (Models.Analytic analytic in analytics)
                    {
                        // Check Output Signals and add them to temp Signals
                        List<int> analyticOutputs = new TableOperations<AnalyticOutputSignal>(connection).QueryRecordsWhere("AnalyticID={0}",analytic.ID)
                            .OrderBy(s => s.OutputIndex)
                            .Select(i => i.ID).ToList();

                        foreach (int id in analyticOutputs)
                            if (!tempSignals.ContainsKey(id))
                                tempSignals.Add(id, Guid.NewGuid().ToString());

                        List<AnalyticInput> analyticInputs = new TableOperations<AnalyticInput>(connection).QueryRecordsWhere("AnalyticID={0}", analytic.ID)
                            .OrderBy(s => s.InputIndex).ToList();


                        IConfiguration config = new ConfigurationBuilder().AddGemstoneConnectionString(analytic.ConnectionString).Build();
                        Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(analytic.AssemblyName));
                        Type type = assembly.GetType(analytic.TypeName);
                        

                        // Setup Analytic
                        result.Sections.Last().Analytics.Add(new AdaptLogic.Analytic()
                        {
                            AdapterType = type,
                            Configuration = config,
                            Outputs = analyticOutputs.Select(id => tempSignals[id]).ToList(),
                            Inputs = analyticInputs.Select(inp =>
                            {
                                if (inp.IsInputSignal)
                                    return inputSignals[inp.SignalID];
                                return tempSignals[inp.SignalID];
                            }).ToList()
                        });
                        

                    }
                }

                List<TemplateOutputSignal> outputs = new TableOperations<TemplateOutputSignal>(connection)
                    .QueryRecordsWhere("TemplateID={0}", template.Id).ToList();

                result.OutputSignals = outputs.Select(s =>
                {
                    int deviceID = 0;
                    if (s.IsInputSignal)
                        deviceID = connection.ExecuteScalar<int>("SELECT DeviceID FROM TemplateInputSignal WHERE ID = {0}", s.SignalID);
                    else
                        deviceID = connection.ExecuteScalar<int>("SELECT DeviceID FROM AnalyticOutputSignal WHERE ID = {0}", s.SignalID);
                    TemplateInputDevice dev = devices.Find(item => item.ID == deviceID);

                    if (s.IsInputSignal)
                        return new AdaptSignal(inputSignals[s.SignalID], s.Name, dev.OutputName);
                    else
                        return new AdaptSignal(tempSignals[s.SignalID], s.Name, dev.OutputName);
                }).ToList();


            }

            result.OutputSignals = result.OutputSignals.GroupBy(c => c.ID, (key, c) => c.FirstOrDefault()).ToList();
            result.InputSignalIds = inputSignals.Select(item => item.Value).Distinct().ToList();
            result.TempSignalIds = tempSignals.Select(item => item.Value).ToList();

            return result;
        }


        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";

        #endregion
    }

    /// <summary>
    /// Enum to indicate state of the Result Page
    /// </summary>
    public enum ResultState
    {
        NoTask = 0,
        Processing = 1,
        View = 2,
    }

}