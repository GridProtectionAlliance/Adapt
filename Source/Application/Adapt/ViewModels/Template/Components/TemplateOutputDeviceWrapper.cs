// ******************************************************************************************************
// TemplateOutputDeviceWrapper.tsx - Gbtc
//
//  Copyright © 2022, Grid Protection Alliance.  All Rights Reserved.
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
//  09/10/2022 - C. Lackner
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
using System.Windows.Input;

namespace Adapt.ViewModels
{
    /// <summary>
    /// A Wrapper for a <see cref="InputDeviceVM"/> to account for Output Display Properties 
    /// </summary>
    public class TemplateOutputDeviceWrapper : ViewModelBase
    {
        #region [ Members ]
        private InputDeviceVM m_Device;
        #endregion

        #region[ Properties ]

        public string Name => m_Device.Name;
        public string OutputName
        { 
            get => m_Device.OutputName;
            set => m_Device.OutputName = value;
        }
        public bool? Enabled
        {
            get 
            {
                if (Signals.Count(s => s.IsOutput && !s.Removed) == Signals.Count(s=> !s.Removed))
                    return true;
                if (Signals.Count(s => s.IsOutput) == 0)
                    return false;
                return null;
            }
            set 
            {
                if (value is null)
                    return;
                Signals.ToList().ForEach(s => s.IsOutput = value ?? true);
            }
        }
        public bool Visible 
        {
            get;
            private set;
        }

        public ObservableCollection<ISignalVM> Signals => new ObservableCollection<ISignalVM>(
                    ((IEnumerable<ISignalVM>)m_Device.Signals).Concat(m_Device.AnalyticSignals)
                    );


        #endregion

        #region[ Constructor ]

        public TemplateOutputDeviceWrapper(InputDeviceVM device)
        {
            m_Device = device;
            m_Device.PropertyChanged += DeviceChange;
            m_Device.SignalPropertyChanged += SignalChanged;

            Visible = !m_Device.Removed && (m_Device.NAnalyticSignals + m_Device.NInputSignals) > 0;
        }
        #endregion

        #region[ Methods ]

        private void DeviceChange(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(m_Device.Name))
                OnPropertyChanged(nameof(Name));
            if (args.PropertyName == nameof(m_Device.OutputName))
                OnPropertyChanged(nameof(OutputName));
            if (args.PropertyName == nameof(m_Device.Removed) || args.PropertyName == nameof(m_Device.NInputSignals) || args.PropertyName == nameof(m_Device.NAnalyticSignals))
            {
                Visible = !m_Device.Removed && (m_Device.NAnalyticSignals + m_Device.NInputSignals) > 0;
                OnPropertyChanged(nameof(Visible));
            }
            if (args.PropertyName == nameof(m_Device.Signals) || args.PropertyName == nameof(m_Device.AnalyticSignals))
            {
                OnPropertyChanged(nameof(Signals));
                OnPropertyChanged(nameof(Enabled));
            }
        }

        private void SignalChanged(object sender, PropertyChangedEventArgs args)
        {
            ISignalVM s = (ISignalVM)sender;
            if (args.PropertyName == nameof(s.IsOutput) || args.PropertyName == nameof(s.Removed))
                OnPropertyChanged(nameof(Enabled));
        }
        #endregion
    }
}