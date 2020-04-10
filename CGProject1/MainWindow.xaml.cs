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
        private bool showing = false;
        private Signal currentSignal;

        private List<Chart> charts = new List<Chart>();
        private Chart activeChannel;

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
                    aboutSignalWindow.UpdateInfo(currentSignal, (int)sliderBegin.Value, (int)sliderEnd.Value);
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

                    chart.Begin = 0;
                    chart.End = currentSignal.SamplesCount;
                }

                activeChannelLabel.Content = "Активный канал:";
                activeChannelPanel.Children.Clear();
                activeChannel = null;
            }
        }

        private void OnChannelClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            var point = Mouse.GetPosition(channels);

            int row = 0;
            double accumulatedHeight = 0.0;

            foreach (var rowDefinition in channels.RowDefinitions) {
                accumulatedHeight += rowDefinition.ActualHeight;
                if (accumulatedHeight >= point.Y)
                    break;
                row++;
            }

            if (currentSignal == null || row >= currentSignal.channels.Length) {
                return;
            }

            foreach (var chart in charts) {
                chart.Selected = false;
                chart.InvalidateVisual();
            }

            charts[row].Selected = true;
            charts[row].InvalidateVisual();

            activeChannel = new Chart(currentSignal.channels[row]);
            activeChannel.Begin = (int)sliderBegin.Value;
            activeChannel.End = (int)sliderEnd.Value;

            activeChannelPanel.Children.Clear();
            activeChannelPanel.Children.Add(activeChannel);
            activeChannelLabel.Content = $"Активный канал: {currentSignal.channels[row].Name}";

            Grid.SetRow(activeChannel, 0);
            Grid.SetColumn(activeChannel, 0);
        }

        private void AboutSignalClick(object sender, RoutedEventArgs e) {
            if (!this.showing)
            {
                aboutSignalWindow = new AboutSignal();
                aboutSignalWindow.UpdateInfo(currentSignal, (int)sliderBegin.Value, (int)sliderEnd.Value);
                aboutSignalWindow.Closed += (object sender, System.EventArgs e) => this.showing = false;
                aboutSignalWindow.Show();
                showing = true;
            }
        }

        private void sliderBegin_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (activeChannel != null) {
                activeChannel.Begin = (int)sliderBegin.Value;
            }

            if (sliderEnd.Value < e.NewValue)
            {
                sliderEnd.Value = e.NewValue;

                if (activeChannel != null) {
                    activeChannel.End = (int)sliderEnd.Value;
                }
            }

            labelBegin.Content = "Begin: " + (int)sliderBegin.Value;
            labelEnd.Content = "End: " + (int)sliderEnd.Value;

            if (aboutSignalWindow != null) {
                aboutSignalWindow.UpdateActiveSegment((int)sliderBegin.Value, (int)sliderEnd.Value);
            }
        }

        private void sliderEnd_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue < sliderBegin.Value)
            {
                (sender as Slider).Value = sliderBegin.Value;
            }

            labelEnd.Content = "End: " + (int)sliderEnd.Value;

            if (activeChannel != null) {
                activeChannel.End = (int)sliderEnd.Value;
            }
            

            if (aboutSignalWindow != null) {
                aboutSignalWindow.UpdateActiveSegment((int)sliderBegin.Value, (int)sliderEnd.Value);
            }
        }
    }
}
