using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Adapt
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		/// <summary>
		/// Called on Application startup to create SQLLite Database etc
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Application_Startup(object sender, StartupEventArgs e)
		{

			MainWindow wnd = new MainWindow();
			wnd.Show();
		}
	}
}
