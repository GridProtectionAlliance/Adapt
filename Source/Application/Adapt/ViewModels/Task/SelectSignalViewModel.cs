// ******************************************************************************************************
//  SelectSignalViewModel.tsx - Gbtc
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
//  09/17/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using Adapt.Models;
using Adapt.View.Template;
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Transactions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for Task Window
    /// </summary>
    public class SelectSignalMappingVM<T>: ViewModelBase 
    {
        #region [ Members ]

        private IEnumerable<T> m_AllOptions;
        private IEnumerable<T> m_ListedOptions;
        private string m_Search;
        private Action<T> m_Complete;
        private Func<T, string> m_TransformDisplay;
        private Func<T,string, bool> m_IncludeSearch;
        #endregion

        #region[ Properties ]


        /// <summary>
        /// The Search Parameter.
        /// </summary>
        public string SearchText 
        { 
            get => m_Search;
            set 
            {
                m_Search = value.Trim();
                Search();
                OnPropertyChanged();
            }
        }

        public string Title { get; }
        public ICommand SelectCommand { get; }

        public int SelectedOption { get; set; }
        public ObservableCollection<string> DisplayList { get; set; }

        public bool ShowError { get; set; }
        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Create a new Selection Window
        /// </summary>
        /// <param name="Complete"> Action to be performed when the Select Button is clicked.</param>
        /// <param name="Search">Function that returns a flag whether the item should be shown with a given Search.</param>
        /// <param name="Display">Function that returns the <see cref="string"/> to be displayed.</param>
        /// <param name="Data">A <see cref="IEnumerable{T}"/> containing the full data</param>
        public SelectSignalMappingVM(Action<T> Complete, Func<T,string,bool> Search, Func<T,string> Display, IEnumerable<T> Data, string Heading="Select a Device")
        {
            m_Search = "";
            
            m_Complete = Complete;
            SelectCommand = new RelayCommand(Select, (obj) => m_ListedOptions.Count() > 0 && SelectedOption >= 0 && SelectedOption < m_ListedOptions.Count());
            m_IncludeSearch = Search;
            m_TransformDisplay = Display;

            m_AllOptions = new ObservableCollection<T>(Data);
            m_ListedOptions = m_AllOptions;
            DisplayList = new ObservableCollection<string>(m_ListedOptions.Select(item => Display.Invoke(item)));
            Title = Heading;
            ShowError = m_AllOptions.Count() == 0;
        }

        #endregion

        #region [ Methods ]
        
        

        /// <summary>
        /// Updates List of Devices when Search changes.
        /// </summary>
        private void Search()
        {
            if (m_Search == "" && m_AllOptions.Count() != m_ListedOptions.Count())
                m_ListedOptions = new ObservableCollection<T>(m_AllOptions);
            if (m_Search.Length > 0)
                m_ListedOptions = new ObservableCollection<T>(m_AllOptions.Where(d => m_IncludeSearch.Invoke(d,m_Search)));

            DisplayList = new ObservableCollection<string>(m_ListedOptions.Select(item => m_TransformDisplay.Invoke(item)));
            OnPropertyChanged(nameof(DisplayList));
        }

        /// <summary>
        /// Closes the window after selecting the Device
        /// </summary>
        private void Select(object window)
        {
            ((SelectSignal)window).Close();
            m_Complete.Invoke(m_ListedOptions.ToList()[SelectedOption]);
        }

        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        
        #endregion
    }
}