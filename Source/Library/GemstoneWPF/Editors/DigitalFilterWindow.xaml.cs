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

namespace GemstoneWPF.Editors
{
    /// <summary>
    /// Interaction logic for DigitalFilterWindow.xaml
    /// </summary>
    public partial class DigitalFilterWindow : Window
    {
        #region [ Members ]

        // Fields
        private object m_currentValue;
        private string m_parameterName;
        private string m_connectionString;
        private Action<object> m_setValue;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="DigitalFilterWindow"/> class.
        /// </summary>
        /// <param name="parameterName">The name of the parameter to be configured.</param>
        /// <param name="currentValue"> The current Value of the parameter.</param>
        /// <param name="completeAction"> The <see cref="Action{object}"/> to be called with the new Parameter.</param>
        public DigitalFilterWindow(string parameterName, object currentValue, Action<object> completeAction)
            : this(parameterName, currentValue, completeAction, null)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DigitalFilterWindow"/> class.
        /// </summary>
        /// <param name="parameterName">The name of the parameter to be configured.</param>
        /// <param name="currentValue"> The current Value of the parameter.</param>
        /// <param name="completeAction"> The <see cref="Action{object}"/> to be called with the new Parameter.</param>
        /// <param name="connectionString">Parameters for the folder browser.</param>
        public DigitalFilterWindow(string parameterName, object currentValue, Action<object> completeAction, string connectionString)
        {
            InitializeComponent();
            m_setValue = completeAction;
            m_parameterName = parameterName;
            m_connectionString = connectionString;
            m_currentValue = currentValue;

            DataContext = new DigitalFilterWindowViewModel();
        }

        #endregion [ Constructors ]

    }
}
