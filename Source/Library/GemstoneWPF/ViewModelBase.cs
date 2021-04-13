// ******************************************************************************************************
//  ViewModelBase.tsx - Gbtc
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
//  03/25/2020 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using System;
using System.ComponentModel;
using System.Windows;

namespace GemstoneWPF
{
    /// <summary>
    /// Base Class for view Models that includes propertyChange Notifier
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Raise property changed event
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event</param>
        public delegate void PropertyChangedEventHandler(object sender, PropertyChangedEventArgs e);

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets a message box to display message to users.
        /// </summary>
        public static Action<string, string, MessageBoxImage> Popup
        {
            get
            {
                return (message, caption, messageBoxImage) => MessageBox.Show(Application.Current.MainWindow, message, caption, MessageBoxButton.OK, messageBoxImage);
            }
        }


        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName()] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
