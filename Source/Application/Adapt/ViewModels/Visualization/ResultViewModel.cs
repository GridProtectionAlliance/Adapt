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
        public async void ProcessTask(AdaptTask task)
        {
            m_processor = new TaskProcessor(task);

            ResultStatus = ResultState.Processing;
            m_progress = new ProcessNotificationVM();
            //m_viewer = new MainVisualizationVM(DateTime.Now, DateTime.Now.AddSeconds(-1));

            m_processor.ReportProgress += (object e, ProgressArgs arg) => {
                if (arg.Complete)
                {
                    ResultStatus = ResultState.View;
                }
                else
                    m_progress.Update(arg);
            };

            m_processor.MessageRecieved += (object e, MessageArgs arg) =>
            {
                m_progress.RecievedMessage(arg);
            };

            OnPropertyChanged(nameof(ProgressVM));
            await m_processor.StartTask();
            m_viewer = new MainVisualizationVM(task,m_processor);
            OnPropertyChanged(nameof(VisualizationVM));
        }

        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
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