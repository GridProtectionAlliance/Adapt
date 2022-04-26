

//******************************************************************************************************
//  ProcessNotificationViewModel.cs - Gbtc
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;


namespace Adapt.ViewModels.Common
{
    /// <summary>
    /// View-model <see cref="Adapt.View.Common.ProcessNotification"/>
    /// </summary>
    public class ProcessNotificationVM : ViewModelBase
    {
        #region [ Members ]
        private string m_message;
        private int m_Progress;
        private ObservableQueue<MessageVM> m_messages;

        public class MessageVM
        {
            public string Text { get; set; }
        }
        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the Message to be Displayed
        /// </summary>
        public string Message => m_message;


        /// <summary>
        /// Gets the Progress (between 0 and 100)
        /// </summary>
        public int Progress => m_Progress;

        /// <summary>
        /// The Messages Displayed in the Progress window
        /// </summary>
        public ObservableQueue<MessageVM> Messages => m_messages;
        private readonly object m_lock = new object();


        #endregion

        #region [ Constructor ]

        public ProcessNotificationVM()
        {
            m_messages = new ObservableQueue<MessageVM>(100);
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(m_messages, m_lock);
            m_Progress = 0;
            m_message = "Loading TaskProcessor...";

        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Updates the Progress Window based on a <see cref="ProgressArgs"/>
        /// </summary>
        /// <param name="arg"></param>
        public void Update(ProgressArgs arg)
        {
            m_Progress = arg.Progress;
            m_message = arg.Message;
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(Message));
        }

        public void RecievedMessage(MessageArgs arg)
        {
             m_messages.Enqueue(new MessageVM() { Text = arg.Message }); 
            OnPropertyChanged(nameof(Messages));
        }
        #endregion
    }
}
