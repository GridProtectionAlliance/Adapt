

//******************************************************************************************************
//  ProcessNotificationWindowViewModel.cs - Gbtc
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
//  04/13/2021 - C. Lackner
//       Created initial Code.
//
//******************************************************************************************************

using Adapt.Models;
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
using System.Windows.Media;


namespace Adapt.ViewModels.Common
{
    /// <summary>
    /// View-model <see cref="Adapt.View.Common.ProcessNotificationWindow"/>
    /// </summary>
    public class ProcessNotificationWindowVM : ViewModelBase
    {
        #region [ Members ]

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the ViewModel
        /// </summary>
        public ProcessNotificationVM ViewModel;
        
        #endregion

        #region [ Constructor ]

        public ProcessNotificationWindowVM()
        {
            ViewModel = new ProcessNotificationVM();
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Updates the Progress Window based on a <see cref="ProgressArgs"/>
        /// </summary>
        /// <param name="arg"></param>
        public void Update(ProgressArgs arg)
        {
            ViewModel.Update(arg);
        }

        #endregion
    }
}
