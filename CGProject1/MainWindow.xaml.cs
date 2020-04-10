using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace CGProject1 {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private AboutSignal aboutSignalWindow;
        private bool showing = false;
        private Signal currentSignal;

        List<Chart> charts = new List<Chart>();

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

                sliderBegin.Minimum = sliderEnd.Minimum = 0;
                sliderBegin.Maximum = sliderEnd.Maximum = currentSignal.SamplesCount;
                sliderBegin.Value = sliderBegin.Minimum;
                sliderEnd.Value = sliderEnd.Minimum;

                foreach(var chart in charts)
                {
                    channels.Children.Remove(chart);
                }
                charts.Clear();

                if (channels.RowDefinitions.Count > 1)
                {
                    channels.RowDefinitions.RemoveRange(1, channels.RowDefinitions.Count - 1);
                }

                for (int i = 0; i < currentSignal.channels.Length; i++)
                {
                    var chart = new Chart(currentSignal.channels[i]);

                    charts.Add(chart);
                    channels.Children.Add(chart);
                    if (channels.RowDefinitions.Count < i + 1)
                    {
                        channels.RowDefinitions.Add(new RowDefinition());
                    }
                    Grid.SetRow(chart, i);
                    Grid.SetColumn(chart, 0);
                }
            }
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

        private void sliderBegin_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            foreach (var chart in charts)
            {
                chart.Begin = (int)sliderBegin.Value;
            }
            if (sliderEnd.Value < e.NewValue)
            {
                sliderEnd.Value = e.NewValue;
                foreach (var chart in charts)
                {
                    chart.End = (int)sliderEnd.Value;
                }
            }

            labelBegin.Content = "Begin: " + (int)sliderBegin.Value;
            labelEnd.Content = "End: " + (int)sliderEnd.Value;
        }

        private void sliderEnd_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue < sliderBegin.Value)
            {
                (sender as Slider).Value = sliderBegin.Value;
            }

            labelEnd.Content = "End: " + (int)sliderEnd.Value;

            foreach (var chart in charts)
            {
                chart.End = (int)sliderEnd.Value;
            }
        }
    }
}
