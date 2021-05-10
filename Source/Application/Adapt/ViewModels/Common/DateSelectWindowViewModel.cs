//******************************************************************************************************
//  DateSelectWindowViewMode.cs - Gbtc
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
//  06/09/2011 - Stephen C. Wills
//       Generated original version of source code.
//  12/20/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//  03/30/2021 - C. Lackner
//       Moved to .NET Core.
//
//******************************************************************************************************

using Adapt.Models;
using Adapt.View.Common;
using Gemstone.Reflection.MemberInfoExtensions;
using Gemstone.StringExtensions;
using GemstoneCommon;
using GemstoneWPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;


namespace Adapt.ViewModels.Common
{
    /// <summary>
    /// View-model <see cref="Adapt.View.Common.DateSelectWindow"/>
    /// </summary>
    public class DateSelectindowVM : ViewModelBase
    {
        #region [ Members ]

        private Action<DateTime, DateTime> m_Confirm;
        private DateSelectVM m_ViewModel;
        private RelayCommand m_ContinueCommand;
        private RelayCommand m_CancelCommand;

        #endregion

        #region [ Properties ]

        public DateSelectVM ViewModel
        {
            get => m_ViewModel;
        }

        public ICommand CancelCommand => m_CancelCommand;
        public ICommand ContinueCommand => m_ContinueCommand;

        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates a New DateSelect Window and performs the specified Action once it is complete
        /// </summary>
        /// <param name="ConfirmTimeRange">The <see cref="Action"/> to be performed with the selected Time range</param>
        public DateSelectindowVM(Action<DateTime,DateTime> ConfirmTimeRange)
        {
            m_ViewModel = new DateSelectVM();
            m_ContinueCommand = new RelayCommand(new Action<object>(Confirm), CanConfirm);
            m_CancelCommand = new RelayCommand(new Action<object>(Cancel), (object w) => true);
            m_Confirm = ConfirmTimeRange;
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// This closes the Window without completing the <see cref="m_Confirm"/> Action.
        /// </summary>
        public void Cancel(object window)
        {
            ((DateSelectWindow)window).Close();
        }

        /// <summary>
        /// This closes the Window and completes the <see cref="m_Confirm"/> Action.
        /// </summary>
        public void Confirm(object window)
        {
            ((DateSelectWindow)window).Close();
            m_Confirm(m_ViewModel.Start, m_ViewModel.End);

        }

        public bool CanConfirm(object window)
        {
            return ViewModel.Start < ViewModel.End;
        }
        #endregion
    }
}