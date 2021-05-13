using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Adapt.View.Visualization
{
    /// <summary>
    /// Interaction logic for VisToolBar.xaml
    /// </summary>
    public partial class VisToolBar : UserControl
    {
        public VisToolBar()
        {
            InitializeComponent();
        }

        private void btnAddPanel_Click(object sender, RoutedEventArgs arg)
        {
            addPanel.IsOpen = true;
            addPanel.Closed += (sender, arg) =>
            {
                btnAddPanel.IsChecked = false;
            };
        }

        private void addPanel_Click(object sender, RoutedEventArgs arg)
        {
            Popup popUp = (Popup)sender;

            if (popUp != null)
                popUp.IsOpen = false;
           
        }

    }
}
