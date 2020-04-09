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
using Microsoft.Win32;

namespace CGProject1 {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private AboutSignal aboutSignalWindow;
        private Signal currentSignal;

        public MainWindow() {
            InitializeComponent();

            aboutSignalWindow = new AboutSignal();
            aboutSignalWindow.UpdateInfo(currentSignal);
        }

        private void AboutClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("КГ-СИСТПРО-1-КАЛИНИН\r\n" +
                "Работу выполнили:\r\n" +
                "Михалев Юрий\r\n" +
                "Калинин Владислав\r\n" +
                "29.02.2020",
                "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenFileClick(object sender, RoutedEventArgs e) {
            var openFileDialog = new OpenFileDialog();
            
            if (openFileDialog.ShowDialog() == true) {
                currentSignal = Parser.Parse(openFileDialog.FileName);
                aboutSignalWindow.UpdateInfo(currentSignal);
            }
        }

        private void AboutSignalClick(object sender, RoutedEventArgs e) {
            aboutSignalWindow.Show();
        }
    }
}
