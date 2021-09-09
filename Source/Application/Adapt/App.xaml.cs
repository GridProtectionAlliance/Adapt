using System.Windows;
using Adapt.DataSources;

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
            PIHistorian.InitializeHost();

            MainWindow wnd = new MainWindow();
            wnd.Show();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            PIHistorian.ShutDownHost();
        }
    }
}
