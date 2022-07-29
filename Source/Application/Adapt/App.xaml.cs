using System;
using System.IO;
using System.Windows;
using Adapt.DataSources;
using Gemstone.IO;
using Serilog;

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

            string localAppDataPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Path.DirectorySeparatorChar}Adapt";
            string localLogPath = $"{localAppDataPath}{Path.DirectorySeparatorChar}Logs";

            if (!Directory.Exists(localAppDataPath))
                Directory.CreateDirectory(localAppDataPath);
            
            if (!Directory.Exists(localLogPath))
                Directory.CreateDirectory(localLogPath);
            
            // Create Logger
            string logFile = $"{localLogPath}{Path.DirectorySeparatorChar}log.txt";

#if DEBUG
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.File(logFile, rollingInterval: RollingInterval.Hour, retainedFileCountLimit: 12).CreateLogger();
#else
            Log.Logger = new LoggerConfiguration().MinimumLevel.Warning().WriteTo.File(logFile, rollingInterval: RollingInterval.Hour, retainedFileCountLimit: 12).CreateLogger();
#endif

            try
            {
                PIHistorian.InitializeHost();
                Log.Logger.Information("Initialized PIHistorian");
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to Initialize PIHistorian");
            }

            // copy database into AppData/Local if it does not exist
            string localAppData = $"{localAppDataPath}{Path.DirectorySeparatorChar}DataBase.db";
            string sourceFile = $"{FilePath.GetAbsolutePath("")}{Path.DirectorySeparatorChar}DataBase.db";



            if (!File.Exists(localAppData))
            {
                Log.Logger.Warning("Overriding Local User Database");
                File.Copy(sourceFile, localAppData, true);
            }


            MainWindow wnd = new MainWindow();
            wnd.Show();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            PIHistorian.ShutDownHost();
        }
    }
}
