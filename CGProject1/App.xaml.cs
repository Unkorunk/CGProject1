using System.Windows;
using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace CGProject1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
            MessageBox.Show($"Error! \n\t {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Logger.Error(e.Exception);
            
            e.Handled = true;
            Current.Shutdown();
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            NLog.LogManager.Shutdown();
        }
    }
}
