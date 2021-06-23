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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Adapt.View.Common
{
    /// <summary>
    /// Interaction logic for hengtest.xaml
    /// </summary>
    public partial class hengtest : UserControl
    {
        public hengtest()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FolderBrowserEditor"/> class.
        /// </summary>
        /// <param name="parameterName">The name of the parameter to be configured.</param>
        /// <param name="currentValue"> The current Value of the parameter.</param>
        /// <param name="completeAction"> The <see cref="Action{object}"/> to be called with the new Parameter.</param>
        /// <param name="connectionString">Parameters for the folder browser.</param>
        public hengtest(string parameterName, object currentValue, Action<object> completeAction, string connectionString)
        {
            InitializeComponent();
            //m_setValue = completeAction;
            //m_parameterName = parameterName;
            //m_connectionString = connectionString;
            //m_currentValue = currentValue;
        }
        private void testtest(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("edit botton clicked.");
        }
    }
}
