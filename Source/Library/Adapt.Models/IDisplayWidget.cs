// ******************************************************************************************************
//  IDisplayWidget.tsx - Gbtc
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
//  05/23/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using System;
using System.Windows;

namespace Adapt.Models
{
    /// <summary>
    /// Represents a Widget used to Display Data.
    /// </summary>
    /// <remarks> <see cref="{T}"/> is providing the data. </remarks>
    public interface IDisplayWidget<T>
    {


        /// <summary>
        /// A <see cref="UIElement"/> used for displaying this Widget.
        /// </summary>
        public UIElement UserControl { get; }

        /// <summary>
        /// Zooms into or out of a section of Data.
        /// </summary>
        /// <param name="end"> The end Time.</param>
        /// <param name="start"> The start Time.</param>
        public void Zoom(DateTime start, DateTime end);

        /// <summary>
        /// Add another Signal to this Widget.
        /// </summary>
        /// <param name="reader"> The reader to obtain the data.</param>
        public void AddReader(T reader);

        /// <summary>
        /// Remove a Signal from this Widget
        /// </summary>
        /// <param name="reader"> The reader to obtain the data.</param>
        public void RemoveReader(T reader);

    }
}
