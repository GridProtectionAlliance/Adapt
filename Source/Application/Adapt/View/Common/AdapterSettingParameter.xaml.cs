﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Adapt.View.Common
{
    /// <summary>
    /// Interaction logic for GeneralSetting.xaml
    /// </summary>
    public partial class AdapterSettingParameter : UserControl
    {
        public AdapterSettingParameter()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext != null)
            { ((dynamic)this.DataContext).Value = ((PasswordBox)sender).Password.ToString(); }
        }
    }
}
