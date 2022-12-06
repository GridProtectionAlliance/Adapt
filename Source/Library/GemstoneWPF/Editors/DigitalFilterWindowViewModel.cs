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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GemstoneWPF.Editors
{
    class DigitalFilterWindowViewModel : ViewModelBase
    {

        #region [ Members ]

        public class CoefficentVM: ViewModelBase
        {
            private double m_value;
            public int Order { get; set; }
            public double Value 
            { 
                get => m_value;
                set
                {
                    m_value = value;
                    OnPropertyChanged();
                }
            }

            public CoefficentVM(int order)
            {
                m_value = 1.0D;
                Order = order;
            }
            public CoefficentVM(int order, double value) : this(order)
            {
                m_value = value;
            }
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

        public ObservableCollection<CoefficentVM> InputCoefficents
        {
            get;
            set;
        }

        public ObservableCollection<CoefficentVM> OutputCoefficents
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
                OutputCoefficents = new ObservableCollection<CoefficentVM>() { new CoefficentVM(0), new CoefficentVM(1) };
                InputCoefficents = new ObservableCollection<CoefficentVM>() { new CoefficentVM(0), new CoefficentVM(1) };
            }
            else
            {
                m_order = flt.Order;
                InputCoefficents = new ObservableCollection<CoefficentVM>(flt.InputCoefficents.Select((v, i) => new CoefficentVM(i, v)));
                OutputCoefficents = new ObservableCollection<CoefficentVM>(flt.OutputCoefficents.Select((v, i) => new CoefficentVM(i, v)));
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

            if (m_order > 10)
                m_order = 10;

            InputCoefficents = new ObservableCollection<CoefficentVM>();
            OutputCoefficents = new ObservableCollection<CoefficentVM>();
            for (int i = 0; i <= m_order; i++)
            {
                InputCoefficents.Add(new CoefficentVM(i));
                OutputCoefficents.Add(new CoefficentVM(i));
            }

            OnPropertyChanged(nameof(InputCoefficents));
            OnPropertyChanged(nameof(OutputCoefficents));
        }

        private void CreateFilter()
        {
            if (InputCoefficents.Count() == 0 || OutputCoefficents.Count() == 0)
            {
                Popup("At least 1 Coefficient has to be specified on the Input and Output side.", "Error", System.Windows.MessageBoxImage.Error);
                return;
            }

            DigitalFilter filter = new DigitalFilter(InputCoefficents.Select(i => i.Value).ToArray(), OutputCoefficents.Select(o => o.Value).ToArray());
            m_completeAction(filter);

        }
        #endregion

    }
}
