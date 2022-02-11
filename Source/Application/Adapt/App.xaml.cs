using System;
using System.IO;
using System.Windows;
using Adapt.DataSources;
using Gemstone.IO;

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

            // copy database into AppData/Local if it does not exist
            string localAppDataPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt";
            string localAppData = $"{localAppDataPath}{Path.DirectorySeparatorChar}DataBase.db";
            string sourceFile = $"{FilePath.GetAbsolutePath("")}{Path.DirectorySeparatorChar}DataBase.db";

            if (!Directory.Exists(localAppDataPath))
                Directory.CreateDirectory(localAppDataPath);

            if (!File.Exists(localAppData))
                File.Copy(sourceFile, localAppData, true);


            MainWindow wnd = new MainWindow();
            wnd.Show();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            PIHistorian.ShutDownHost();
        }
    }
}
