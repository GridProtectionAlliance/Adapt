// ******************************************************************************************************
// AnalyticInputViewModel.tsx - Gbtc
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
//  08/13/2021 - C. Lackner
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Adapt.ViewModels
{
    /// <summary>
    /// ViewModel for selecting a Signal in a Template
    /// </summary>
    public class SelectSignalVM: ViewModelBase
    {
        #region [ Classes ]

        /// <summary>
        /// Wrapper class for <see cref="InputDeviceVM"/> to allow filtering for specific conditions such as Section Order
        /// </summary>
        public class InputDeviceWrapperVM : ViewModelBase
        {
            public ObservableCollection<ISignalVM> Signals { get; }
            public string Name => m_device.Name;
            public int NSignals => m_device.NInputSignals + m_device.NAnalyticSignals;

            private InputDeviceVM m_device;
            public InputDeviceWrapperVM(InputDeviceVM device, Func<ISignalVM, bool> filter)
            {
                m_device = device;
                Signals = new ObservableCollection<ISignalVM>(
                    ((IEnumerable<ISignalVM>)device.Signals.Where(s => !s.Removed && filter(s))).Concat(device.AnalyticSignals.Where(s => !s.Removed && filter(s)))
                    );

            }
        }

        #endregion

        #region [ Members ]

        private Action<ISignalVM> m_onComplete;
        private ISignalVM m_selectedItem;

        #endregion

        #region[ Properties ]

        /// <summary>
        /// The <see cref="ICommand"/> called by the Select Button.
        /// </summary>
        public ICommand SelectCommand { get; }

        /// <summary>
        /// ViewModels for all available <see cref="TemplateInputDevice"/>.
        /// </summary>
        public ObservableCollection<InputDeviceWrapperVM> Devices { get; }

        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Creates a new <see cref="SelectSignalVM"/> VieModel.
        /// </summary>
        /// <param name="template"> The <see cref="TemplateVM"/> used to get available Signals. </param>
        /// <param name="onComplete">The <see cref="Action"/> to be called when a Signal is selected. </param>
        /// <param name="filter">The maximum Order of the Sections. -1 for all Sections in the <paramref name="templateViewModel"/>. </param>
        public SelectSignalVM(TemplateVM template, Action<ISignalVM> onComplete, Func<ISignalVM,bool> filter)
        {
            m_onComplete = onComplete;

            // Create Full List of Signals including outputs from Analytics
            Devices = new ObservableCollection<InputDeviceWrapperVM>(template.Devices.Where(d => !d.Removed).Select(item => new InputDeviceWrapperVM(item, filter)).Where(d => d.Signals.Count() > 0));
            m_selectedItem = Devices.FirstOrDefault()?.Signals.FirstOrDefault();
            SelectCommand = new RelayCommand(Select, (obj) => !(m_selectedItem is null));
        }

        #endregion

        #region [ Methods ]
        
        /// <summary>
        /// Creates the <see cref="AnalyticInput"/> and then closes the Window.
        /// </summary>
        /// <param name="window"> The <see cref="SelectSignalWindow"/>. </param>
        private void Select(object window)
        {
            m_onComplete.Invoke(m_selectedItem);
        }

        /// <summary>
        /// Selection Handler for selecting a Signal or Device
        /// </summary>
        /// <param name="e"> The EventArguments associated with the onClick Event</param>
        /// <param name="sender"> The <see cref="TreeViewItem"/> where this event is triggered. </param>
        public void OnSelectItem(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView tv = sender as TreeView;
            if (tv != null)
                    m_selectedItem = tv.SelectedItem as ISignalVM;
        }

        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt{Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion
    }
}