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
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for Task Window
    /// </summary>
    public class SelectMappingVM<T>: ViewModelBase 
    {
        #region [ Members ]

        public class ItemWrapper<T>: ViewModelBase
        {
            private bool m_selected;
            private bool m_visible;
            private Action<ItemWrapper<T>> m_singleSelect;
            public ItemWrapper(T item, Func<T, string> display, bool showCheckBox, Action<ItemWrapper<T>> singleSelect)
            {
                Display = display(item);
                m_selected = false;
                Item = item;
                m_visible = true;
                ShowCheckBox = showCheckBox;
                Select = new RelayCommand(() => { Selected = !Selected; }, () => true);
                m_singleSelect = singleSelect;
            }
            public string Display { get; }
            public T Item { get; }
            public bool Selected
            { 
                get => m_selected; 
                set {
                    if (!ShowCheckBox && value)
                        m_singleSelect.Invoke(this);
                    m_selected = value; 
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Visible));
                    OnPropertyChanged(nameof(Color));
                }
            }
            
            public bool Visible 
            {
                get => m_selected || m_visible; 
                set {
                    m_visible = value; 
                    OnPropertyChanged();
                } 
            }

            public bool ShowCheckBox { get; }

            public Brush Color => (m_selected ? SystemColors.HighlightBrush : SystemColors.WindowBrush);

            public ICommand Select { get; }
        }

        private string m_Search;
        private Action<List<T>> m_Complete;
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

        public ICommand SelectAll { get; }

        public ObservableCollection<ItemWrapper<T>> DisplayList { get; set; }

        public bool ShowError { get; set; }

        public bool MultiSelect { get; }

        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Create a new Selection Window
        /// </summary>
        /// <param name="Complete"> Action to be performed when the Select Button is clicked.</param>
        /// <param name="Search">Function that returns a flag whether the item should be shown with a given Search.</param>
        /// <param name="Display">Function that returns the <see cref="string"/> to be displayed.</param>
        /// <param name="Data">A <see cref="IEnumerable{T}"/> containing the full data</param>
        /// <param name="Heading">The Heading shown in the window</param>
        /// <param name="allowMultiSelect"> If <see cref="true"/> the window will allow selection of multiple Items. </param>
        public SelectMappingVM(Action<List<T>> Complete, Func<T,string,bool> Search, Func<T,string> Display, IEnumerable<T> Data, string Heading="Select a Device", bool allowMultiSelect=false)
        {
            m_Search = "";
            
            m_Complete = Complete;
            SelectCommand = new RelayCommand(Select, () => DisplayList.Count(item => item.Selected) > 0);
            m_IncludeSearch = Search;
            
            DisplayList = new ObservableCollection<ItemWrapper<T>>(Data.Select(item => new ItemWrapper<T>(item,Display, allowMultiSelect, SingleSelect)));
            Title = Heading;
            ShowError = Data.Count() == 0;
            MultiSelect = allowMultiSelect;
            if (MultiSelect)
                SelectAll = new RelayCommand(() => { 
                    DisplayList.ToList().ForEach(d => d.Selected = true);
                }, () => true);
            else
                SelectAll = new RelayCommand(() => { }, () => true);
        }

        #endregion

        #region [ Methods ]
        
        

        /// <summary>
        /// Updates List of Devices when Search changes.
        /// </summary>
        private void Search()
        {
            if (m_Search == "" && DisplayList.Any(d => !d.Visible))
                DisplayList.ToList().ForEach(d => d.Visible = true);
            if (m_Search.Length > 0)
                DisplayList.ToList().ForEach(d => d.Visible = m_IncludeSearch.Invoke(d.Item,m_Search));

            OnPropertyChanged(nameof(DisplayList));
        }

        /// <summary>
        /// Closes the window after selecting the Device
        /// </summary>
        private void Select()
        {
            m_Complete.Invoke(DisplayList.Where(d => d.Selected).Select( d => d.Item).ToList());
        }

        private void SingleSelect(ItemWrapper<T> item)
        {
            foreach (ItemWrapper<T> i in DisplayList)
                if (i != item)
                    i.Selected = false;
        }
        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        
        #endregion
    }
}