//******************************************************************************************************
//  HeightSubtraction.cs - Gbtc
//
//  Copyright © 2021, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  07/26/2021 C. Lackner
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Adapt.Converter
{
    /// <summary>
    /// Represents an <see cref="IValueConverter"/> class, which takes object value and subtracts a number.
    /// </summary>
    public class HeightSubtraction : IValueConverter
    {
        #region [ IValueConverter Members ]

        /// <summary>
        /// Converts object value to double and subtracts a number.
        /// </summary>
        /// <param name="value">Object value to be converted.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The number to subtract.</param>
        /// <param name="culture">The culture to use in conversion.</param>
        /// <returns><see cref="double"/> with <see cref="parameter"/> Subtracted.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double val = 0;
            double sub = 0;
            if (!double.TryParse(value.ToString(), out val))
                return 0;
            if (!double.TryParse(parameter.ToString(), out sub))
                return val;

            return val - sub;
        }

        /// <summary>
        /// Converts <see cref="System.Windows.Visibility"/> to object.
        /// </summary>
        /// <param name="value"><see cref="System.Windows.Visibility"/> value to be converted.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use in conversion.</param>
        /// <param name="culture">The culture to use in conversion.</param>
        /// <returns>object value.</returns>
        /// <remarks>This method has not been implemented.</remarks>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        #endregion

    }
}
