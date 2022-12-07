﻿// ******************************************************************************************************
//  ExpanderToVisibility.tsx - Gbtc
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
//  05/12/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using Adapt.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Adapt.Converter
{
    class ExpanderToVisibility : IValueConverter
    {
        // <summary>
        /// Converts <see cref="SelectedExpander"/> value to <see cref="System.Windows.Visibility"/> enumeration.
        /// </summary>
        /// <param name="value">Object value to be converted.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use in conversion.</param>
        /// <param name="culture">The culture to use in conversion.</param>
        /// <returns><see cref="System.Windows.Visibility"/> enumeration.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SelectedExpander val;
            SelectedExpander param;
            if (!Enum.TryParse<SelectedExpander>(value.ToString(), out val))
                return Visibility.Collapsed;
            if (!Enum.TryParse<SelectedExpander>(parameter.ToString(), out param))
                return Visibility.Collapsed;
            if (val == param)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
