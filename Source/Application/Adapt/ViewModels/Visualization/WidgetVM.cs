// ******************************************************************************************************
//  MainVisualizationViewModel.tsx - Gbtc
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
using Adapt.Models;
using Adapt.ViewModels.Visualization.Widgets;
using AdaptLogic;
using Gemstone.IO;
using Gemstone.Reflection.MemberInfoExtensions;
using Gemstone.StringExtensions;
using Gemstone.TypeExtensions;
using GemstoneWPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Adapt.ViewModels.Vizsalization
{
    /// <summary>
    /// Primary ViewModel for a Widget. Just wraps an <see cref="IDisplayWidget"/>
    /// </summary>
    public class WidgetVM : ViewModelBase
    {
        #region [ Members ]

        private IDisplayWidget m_widget;
        private MainVisualizationVM m_parent;

        public class AvailableReader: ViewModelBase
        {
            private IReader m_reader;
            private WidgetVM m_widget;

            public bool Selected 
            {
                get; 
                set;
            }
            public string Name => m_reader.Signal.Name;

            public ICommand Select { get; }
            public AvailableReader(WidgetVM widgetVM, SignalReader reader, bool selected = false)
            {
                m_reader = reader;
                Selected = selected;
                m_widget = widgetVM;
                Select = new RelayCommand(() => OnSelect(), () => true);
                if (selected)
                    m_widget.AddReader(m_reader);
            }

            
            public void OnSelect()
            {
                if (Selected)
                {
                    Selected = false;
                    m_widget.RemoveReader(m_reader);
                }
                else
                {
                    Selected = true;
                    m_widget.AddReader(m_reader);
                }
                
                OnPropertyChanged(nameof(Selected));
            }
        }

        public class ContextMenueVM: ViewModelBase
        {
            public string Text { get; }

            public bool Selected { get; set; }

            public ObservableCollection<ContextMenueVM> SubMenue { get; }

            public ICommand Command { get; }
            public ContextMenueVM(string message, bool selected, Action<bool> onClick)
            {
                Text = message;
                Selected = selected;
                SubMenue = new ObservableCollection<ContextMenueVM>();
                Command = new RelayCommand(() => {
                    Selected = !Selected;  
                    onClick.Invoke(Selected);
                });
            }

            public ContextMenueVM(string message, IEnumerable<ContextMenueVM> subMenues )
            {
                Text = message;
                Selected = false;
                SubMenue = new ObservableCollection<ContextMenueVM>(subMenues);
                Command = new RelayCommand(() => { });
            }
        }

        #endregion

        #region[ Properties ]
        public IDisplayWidget Widget => m_widget;
        public UIElement UserControl => m_widget.UserControl;

        public ObservableCollection<ContextMenueVM> ContextMenue { get; set; }

        /// <summary>
        /// Event that gets triggered when the User changes the Window.
        /// </summary>
        public event EventHandler<ZoomEventArgs> ChangedWindow;

        #endregion

        #region[ Constructor]
        public WidgetVM(MainVisualizationVM parent, IDisplayWidget widget, DateTime start, DateTime end, Dictionary<string,List<SignalReader>> dataReader)
        {
            m_widget = widget;
            m_parent = parent;
            m_widget.Zoom(start, end);
            m_widget.ChangedWindow += WindowChanged;

            CreateContextMenue(dataReader);

        }

        #endregion

        #region [ Methods ]

        public void Zoom(DateTime start, DateTime end) => m_widget.Zoom(start, end);

        private void AddReader(IReader reader) => m_widget.AddReader(reader);

        private void RemoveReader(IReader reader) => m_widget.RemoveReader(reader);

        private void WindowChanged(object sender, ZoomEventArgs args) => ChangedWindow.Invoke(sender, args);
       
        private void CreateContextMenue(Dictionary<string, List<SignalReader>> readers)
        {
            bool isInitial = true;
            List<ContextMenueVM> menue = new List<ContextMenueVM>();

            foreach (string device in readers.Keys)
            {
                if (!readers[device].Any(s => m_widget.AllowSignal(s.Signal)))
                    continue;
                menue.Add(new ContextMenueVM(device, readers[device].Where(s => m_widget.AllowSignal(s.Signal))
                    .Select(s => new ContextMenueVM(s.Signal.Name,isInitial,(bool selected) => { if (selected) m_widget.AddReader(s); else m_widget.RemoveReader(s); }))));
                if (isInitial)
                    readers[device].Where(s => m_widget.AllowSignal(s.Signal)).Select(s => { m_widget.AddReader(s); return 1; });

                isInitial = false;
            }

            menue.Add(new ContextMenueVM("Remove Widget", false, (bool selected) => { RemoveWidget(); }));
            ContextMenue = new ObservableCollection<ContextMenueVM>(menue);
            OnPropertyChanged(nameof(ContextMenue));
        }

        private void RemoveWidget()
        {
            m_parent.RemoveWidget(this);
        }
        #endregion

        #region [ static ]

        #endregion
    }


}