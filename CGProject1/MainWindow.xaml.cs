using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CGProject1.Chart;
using Microsoft.Win32;

namespace CGProject1 {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private AboutSignal aboutSignalWindow;
        private Signal currentSignal;

        List<Canvas> canvases = new List<Canvas>();
        List<ChartController> controllers = new List<ChartController>();

        public MainWindow() {
            InitializeComponent();

            aboutSignalWindow = new AboutSignal();
            aboutSignalWindow.UpdateInfo(currentSignal);
        }

        private void AboutClick(object sender, RoutedEventArgs e) {
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

                controllers.Clear();

                foreach(var canvas in canvases)
                {
                    grid.Children.Remove(canvas);
                }
                canvases.Clear();

                if (grid.RowDefinitions.Count > 1)
                {
                    grid.RowDefinitions.RemoveRange(1, grid.RowDefinitions.Count - 1);
                }

                for (int i = 0; i < currentSignal.channels.Length; i++)
                {
                    var canvas = new Canvas();
                    canvas.Width = grid.ActualWidth;
                    canvas.Height = grid.ActualHeight / currentSignal.channels.Length;
                    canvases.Add(canvas);
                    grid.Children.Add(canvas);
                    if (grid.RowDefinitions.Count < i + 1)
                    {
                        grid.RowDefinitions.Add(new RowDefinition());
                    }
                    Grid.SetRow(canvas, i);
                    Grid.SetColumn(canvas, 0);
                    controllers.Add(new ChartController(canvas, currentSignal.channels[i]) { 
                        Begin = 0,
                        End = 10000
                    });
                }
            }
        }

        private void AboutSignalClick(object sender, RoutedEventArgs e) {
            aboutSignalWindow.Show();
        }
    }
}
