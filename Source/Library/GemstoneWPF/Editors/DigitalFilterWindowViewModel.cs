// ******************************************************************************************************
//  DigitalFilterWindowViewModel.tsx - Gbtc
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
//  04/15/2022 - C. Lackner
//       Generated original version of source code.
//
// ******************************************************************************************************

using GemstoneAnalytic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static GemstoneWPF.Editors.DigitalFilterWindowViewModel;

namespace GemstoneWPF.Editors
{
    class DigitalFilterWindowViewModel : ViewModelBase
    {
        #region [ Members ]

        public class CoeffientSet: ViewModelBase, IEditableObject
        {
            private int m_order;
            private double m_input;
            private double m_output;

            public int Order 
            {
                get => m_order;
            }

            public double OutputCoefficent
            {
                get => m_output;
                set
                {
                    m_output = value;
                    OnPropertyChanged();
                }
            }

            public double InputCoefficent
            {
                get => m_input;
                set
                {
                    m_input = value;
                    OnPropertyChanged();
                }
            }

            public CoeffientSet(int index)
            {
                m_order = index;
                m_input = 1.0D;
                m_output = 1.0D;
            }

            public void BeginEdit() {}

            public void CancelEdit() {}

            public void EndEdit() {}
        }

        private Action<object> m_completeAction; 
        #endregion

        #region [ Properties ]

        private int m_order = 1;
        
        /// <summary>
        /// The order of the Filter
        /// </summary>
        public int Order
        {
            get => m_order;
            set 
            {
                m_order = value;
                OnOrderChange();
                OnPropertyChanged(); 
            }
        }

        /// <summary>
        /// Flag indicating whether the formula should be shown or if the filter should be shown as a Table of coefficents
        /// </summary>
        public bool ShowSimple => m_order < 5;

        public ObservableCollection<CoeffientSet> Coeffients 
        {
            get;
            set;
        }

        public ICommand CreateCommand { get; set; }
        #endregion

        #region [ Constructor ]

        public DigitalFilterWindowViewModel(object currentValue, Action<object> completeAction)
        {
           
            DigitalFilter flt = (DigitalFilter)currentValue;
            if (flt is null)
            {

                m_order = 1;
                Coeffients = new ObservableCollection<CoeffientSet>() { new CoeffientSet(0), new CoeffientSet(1) };
               
            }
            else
            {
                m_order = flt.Order;
                List<CoeffientSet> coefficents = new List<CoeffientSet>();
                for (int i=0; i <= m_order; i++)
                {
                    CoeffientSet cof = new CoeffientSet(i);

                    if (flt.InputCoefficents.Count() > i)
                        cof.InputCoefficent = flt.InputCoefficents[i];
                    else
                        cof.InputCoefficent = 0.0D;

                    if (flt.OutputCoefficents.Count() > i)
                        cof.OutputCoefficent = flt.OutputCoefficents[i];
                    else
                        cof.OutputCoefficent = 0.0D;

                    coefficents.Add(cof);
                }
                Coeffients = new ObservableCollection<CoeffientSet>(coefficents);
            }

            CreateCommand = new RelayCommand(() => CreateFilter(), () => true);
            m_completeAction = completeAction;
        }
        #endregion

        #region [ Methods ]

        private void OnOrderChange()
        {
            if (m_order < 1)
                m_order = 1;

            if (m_order > 100)
                m_order = 100;

            Coeffients = new ObservableCollection<CoeffientSet>();
           
            for (int i = 0; i <= m_order; i++)
            {
                Coeffients.Add(new CoeffientSet(i));
            }

            OnPropertyChanged(nameof(Coeffients));
            OnPropertyChanged(nameof(ShowSimple));
        }

        private void CreateFilter()
        {
            if (Coeffients.Count() == 0 || !Coeffients.Any(c => c.InputCoefficent != 0.0D) || !Coeffients.Any(c => c.OutputCoefficent != 0.0D))
            {
                Popup("At least 1 non-zero Coefficient has to be specified on the Input and Output side.", "Error", System.Windows.MessageBoxImage.Error);
                return;
            }

            DigitalFilter filter = new DigitalFilter(Coeffients.Select(i => i.InputCoefficent).ToArray(), Coeffients.Select(o => o.OutputCoefficent).ToArray());
            m_completeAction(filter);

        }

        
  
        #endregion

    }
}
