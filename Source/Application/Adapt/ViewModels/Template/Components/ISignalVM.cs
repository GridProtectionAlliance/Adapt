// ******************************************************************************************************
// ISignalVM.tsx - Gbtc
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
    /// An Interface to represent common Features of <see cref="AnalyticOutputVM"/> and <see cref="InputSignalVM"/>
    /// </summary>
    public interface ISignalVM
    {
        /// <summary>
        /// Indicates if this <see cref="ISignalVM"/> is a Template Input
        /// </summary>
        bool IsInput { get; }

        /// <summary>
        /// If <see cref="IsInput"/> is false this indicates the <see cref="SectionVM.Order"/>
        /// </summary>
        int SectionOrder { get; }

        /// <summary>
        /// The ID of the associated Model. Returns <see cref="{Null}"/> if no model exists
        /// </summary>
        int? ID { get; }

        /// <summary>
        /// The Display Name of the Signal
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Flag indicating if the Signal is Selected as an Output
        /// </summary>
        bool IsOutput { get; set; }

        /// <summary>
        /// The Name used as Output
        /// </summary>
        string OutputName { get; set; }

        /// <summary>
        /// Required for WPF Property Change Notifications. This comes from <see cref="ViewModelBase"/>
        /// </summary>
        event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Flag to indicate if the Signal has been removed
        /// </summary>
        bool Removed { get; }

    }
}