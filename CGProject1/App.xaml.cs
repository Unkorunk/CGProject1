using System.Windows;

namespace CGProject1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
            MessageBox.Show($"Error! \n\t {e.Exception.Message} \n\t Details in {Logger.LogFilename} from {Logger.Instance.Path}");
            Logger.Instance.Log($"Error occured {e.Exception.Message} \n\t {e.Exception.StackTrace}", Logger.LogType.Error);
            e.Handled = true;
            Current.Shutdown();
        }

        private void Application_Startup(object sender, StartupEventArgs e) {
            Logger.StartLogger();
        }

        private void Application_Exit(object sender, ExitEventArgs e) {
            Logger.Instance.Dispose();
        }
    }
}
