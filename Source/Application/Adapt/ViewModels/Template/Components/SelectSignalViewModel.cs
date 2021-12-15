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
        /// ViewModel for an available Device
        /// </summary>
        public class DeviceVM: ViewModelBase
        {
            public string Name { get; }
            public int NSignals => Signals.Count();
            public ObservableCollection<SignalVM> Signals { get; private set; }
            public int ID { get; }
            public DeviceVM(InputDeviceVM inputDeviceViewModel)
            {
                Name = inputDeviceViewModel.Name;
                Signals = new ObservableCollection<SignalVM>(inputDeviceViewModel.Signals.Where(s => !s.Removed).Select(s => new SignalVM(s)));
                ID = inputDeviceViewModel.ID;

            }
            /// <summary>
            /// Adds Output Signals from other Analytics to this Device.
            /// </summary>
            public void AddSignals(IEnumerable<AnalyticOutputVM> signals)
            {
                foreach (AnalyticOutputVM s in signals)
                    Signals.Add(new SignalVM(s));
                
                OnPropertyChanged(nameof(NSignals));
                OnPropertyChanged(nameof(Signals));
            }
        }

        /// <summary>
        /// ViewModel for an available Signal
        /// </summary>
        public class SignalVM: ViewModelBase
        {
            public string Name { get; }
            public int ID { get; }
            public bool IsInput { get; }
            public SignalVM(AnalyticOutputVM signal)
            {
                Name = signal.Name;
                ID = signal.ID;
                IsInput = false;
            }
            public SignalVM(InputSignalVM signalVM)
            {
                Name = signalVM.Name;
                ID = signalVM.ID;
                IsInput = true;
            }
        }
        #endregion
        #region [ Members ]

        private Action<AnalyticInput> m_onComplete;
        private int m_selectedSignalID;
        private bool m_selectedInputSignal;
        #endregion

        #region[ Properties ]

        /// <summary>
        /// The <see cref="ICommand"/> called by the Cancel Button.
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// The <see cref="ICommand"/> called by the Select Button.
        /// </summary>
        public ICommand SelectCommand { get; }

        /// <summary>
        /// ViewModels for all available <see cref="TemplateInputDevice"/>.
        /// </summary>
        public ObservableCollection<DeviceVM> Devices { get; private set; }
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Creates a new <see cref="SelectSignalVM"/> VieModel.
        /// </summary>
        /// <param name="templateViewModel"> The <see cref="TemplateVM"/> used to get available Signals. </param>
        /// <param name="onComplete">The <see cref="Action"/> to be called when a Signal is selected. </param>
        /// <param name="maxOrder">The maximum Order of the Sections. -1 for all Sections in the <paramref name="templateViewModel"/>. </param>
        public SelectSignalVM(TemplateVM templateViewModel, Action<AnalyticInput> onComplete, int maxOrder=-1)
        {
            CancelCommand = new RelayCommand(Cancel, (obj) => true);
            SelectCommand = new RelayCommand(Select, (obj) => true);
            m_onComplete = onComplete;
               
            // Create Full List of Signals including outputs from Analytics
            Devices = new ObservableCollection<DeviceVM>(templateViewModel.Devices.Where(d => !d.Removed).Select(d => new DeviceVM(d)));
            IEnumerable<AnalyticOutputVM> outputs = templateViewModel.Sections
                .Where(s => s.Order < maxOrder || maxOrder == -1)
                .SelectMany(s => s.Analytics).SelectMany(a => a.Outputs);

            foreach(DeviceVM dev in Devices)
            {
                dev.AddSignals(outputs.Where(s => s.DeviceID == dev.ID));
            
            }

            m_selectedSignalID = Devices.First().Signals.First().ID;
            m_selectedInputSignal = Devices.First().Signals.First().IsInput;
        }

        #endregion

        #region [ Methods ]
        
        /// <summary>
        /// Closes the Window.
        /// </summary>
        /// <param name="window"> The <see cref="SelectSignalWindow"/>. </param>
        private void Cancel(object window)
        {
            ((SelectSignalWindow)window).Close();
        }

        /// <summary>
        /// Creates the <see cref="AnalyticInput"/> and then closes the Window.
        /// </summary>
        /// <param name="window"> The <see cref="SelectSignalWindow"/>. </param>
        private void Select(object window)
        {
            AnalyticInput result = new AnalyticInput() {
                IsInputSignal = m_selectedInputSignal,
                SignalID = m_selectedSignalID,

            };

            m_onComplete.Invoke(result);
            Cancel(window);
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
            {
                SignalVM tvi = tv.SelectedItem as SignalVM;
                if (tvi != null)
                {
                    m_selectedSignalID = tvi.ID;
                    m_selectedInputSignal = tvi.IsInput;


                }
            }
        }

        #endregion

        #region [ Static ]

        private static readonly string ConnectionString = $"Data Source={FilePath.GetAbsolutePath("") + Path.DirectorySeparatorChar}DataBase.db; Version=3; Foreign Keys=True; FailIfMissing=True";
        private static readonly string DataProviderString = "AssemblyName={System.Data.SQLite, Version=1.0.109.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139}; ConnectionType=System.Data.SQLite.SQLiteConnection; AdapterType=System.Data.SQLite.SQLiteDataAdapter";
        #endregion
    }
}