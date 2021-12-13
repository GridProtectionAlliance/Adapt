// ******************************************************************************************************
//  WidgetBaseViewModel.tsx - Gbtc
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
//  05/13/2021 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************
using Adapt.Models;
using AdaptLogic;
using GemstoneWPF;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Adapt.ViewModels.Visualization.Widgets
{
    public abstract class WidgetBaseVM: ViewModelBase, IDisplayWidget
    {

        protected List<IReader> m_readers;
        protected DateTime m_start;
        protected DateTime m_end;


        #region [ Properties ]
        public abstract UIElement UserControl { get; }
        public event EventHandler<ZoomEventArgs> ChangedWindow;

        #endregion

        #region [ constructor ]
        public WidgetBaseVM()
        {
            m_readers = new List<IReader>();
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Zooms in to a new Section.
        /// </summary>
        /// <param name="start">new Start Time.</param>
        /// <param name="end">new End Time.</param>
        public virtual void Zoom(DateTime start, DateTime end)
        {
            if (start < end)
            {
                m_start = start;
                m_end = end;
            }
            else 
            {
                m_end = start;
                m_start = end;
            }
        }

        public virtual void AddReader(IReader reader)
        {
            m_readers.Add(reader);
        }

        public virtual void RemoveReader(IReader reader)
        {
            int index = m_readers.FindIndex(r => reader.SignalGuid == r.SignalGuid);
            if (index > -1)
                m_readers.RemoveAt(index);
        }

        public virtual bool AllowSignal(AdaptSignal signal) => true;
       
        #endregion

    }
}
