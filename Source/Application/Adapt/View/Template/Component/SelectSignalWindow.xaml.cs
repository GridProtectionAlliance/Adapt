using Adapt.ViewModels;
using System;
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
using System.Windows.Shapes;

namespace Adapt.View.Template
{
    /// <summary>
    /// Interaction logic for SelectSignalWindow.xaml
    /// </summary>
    public partial class SelectSignalWindow : Window
    {
        public SelectSignalWindow()
        {
            InitializeComponent();
        }
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectSignalVM parentViewModel = (SelectSignalVM)this.DataContext;
            parentViewModel.OnSelectItem(sender, e);
        }
    }
}
