using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace CGProject1 {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private AboutSignal aboutSignalWindow;
        private Oscillograms oscillogramWindow;

        private bool showing = false;
        private bool isOscillogramShowing = false;
        private Signal currentSignal;

        private List<Chart> charts = new List<Chart>();
        private Chart activeChannelInGrid;

        public MainWindow() {
            InitializeComponent();
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
                if (aboutSignalWindow != null)
                {
                    aboutSignalWindow.UpdateInfo(currentSignal);
                }

                foreach(var chart in charts)
                {
                    channels.Children.Remove(chart);
                }
                charts.Clear();

                //if (channels.RowDefinitions.Count > 1)
                //{
                //    channels.RowDefinitions.RemoveRange(1, channels.RowDefinitions.Count - 1);
                //}

                if (isOscillogramShowing) {
                    oscillogramWindow.Update(currentSignal);
                }

                for (int i = 0; i < currentSignal.channels.Length; i++)
                {
                    var chart = new Chart(currentSignal.channels[i]);
                    chart.Height = 100;

                    charts.Add(chart);
                    channels.Children.Add(chart);

                    chart.ContextMenu = new ContextMenu();

                    var item1 = new MenuItem();
                    item1.Header = "Осциллограмма";
                    int cur = i;
                    item1.Click += (object sender, RoutedEventArgs args) => {
                        OpenOscillograms();

                        oscillogramWindow.AddChannel(currentSignal.channels[cur]);
                    };

                    chart.ContextMenu.Items.Add(item1);

                    //if (channels.RowDefinitions.Count < i + 1)
                    //{
                        
                    //    channels.RowDefinitions.Add(new RowDefinition());
                    //}

                    //Grid.SetRow(chart, i);
                    //Grid.SetColumn(chart, 0);

                    chart.Begin = 0;
                    chart.End = currentSignal.SamplesCount;
                }
            }
        }

        private void OnChannelClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            var point = Mouse.GetPosition(channels);

            int row = 0;
            double accumulatedHeight = 0.0;

            foreach (var chart in charts) {
                accumulatedHeight += chart.ActualHeight;
                if (accumulatedHeight >= point.Y)
                    break;
                row++;
            }

            if (currentSignal == null || row >= currentSignal.channels.Length) {
                return;
            }

            foreach (var chart in charts) {
                chart.Selected = false;
                chart.DisableSelectInterval();
                chart.InvalidateVisual();
            }

            activeChannelInGrid = charts[row];
            charts[row].Selected = true;
            //charts[row].SetSelectInterval((int)sliderBegin.Value, (int)sliderEnd.Value);
            charts[row].EnableSelectInterval();
            charts[row].InvalidateVisual();
        }

        private void AboutSignalClick(object sender, RoutedEventArgs e) {
            if (!this.showing)
            {
                aboutSignalWindow = new AboutSignal();
                aboutSignalWindow.UpdateInfo(currentSignal);
                aboutSignalWindow.Closed += (object sender, System.EventArgs e) => this.showing = false;
                aboutSignalWindow.Show();
                showing = true;
            }
        }

        private void OscillogramsClick(object sender, RoutedEventArgs e) {
            OpenOscillograms();
        }

        private void OpenOscillograms() {
            if (!this.isOscillogramShowing) {
                isOscillogramShowing = true;
                oscillogramWindow = new Oscillograms();
                oscillogramWindow.Closed += (object sender, System.EventArgs e) => this.isOscillogramShowing = false;
                oscillogramWindow.Update(currentSignal);
                oscillogramWindow.Show();
            }
        }
    }
}
