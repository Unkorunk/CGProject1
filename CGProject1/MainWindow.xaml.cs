using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace CGProject1 {
    public partial class MainWindow : Window {
        public static MainWindow instance = null;

        private AboutSignal aboutSignalWindow;
        private Oscillograms oscillogramWindow;
        private ModelingWindow modelingWindow;

        private bool showing = false;
        private bool isOscillogramShowing = false;
        private bool isModelingWindowShowing = false;
        public Signal currentSignal;

        private List<Chart> charts = new List<Chart>();
        private Chart activeChannelInGrid;

        public MainWindow() {
            instance = this;
            InitializeComponent();
            this.Closed += (object sender, System.EventArgs e) => {
                if (aboutSignalWindow != null) {
                    aboutSignalWindow.Close();
                }

                if (modelingWindow != null) {
                    modelingWindow.Close();
                }

                if (isOscillogramShowing) {
                    oscillogramWindow.Close();
                }
            };
        }

        private void AboutClick(object sender, RoutedEventArgs e) {
            MessageBox.Show("КГ-СИСТПРО-1-КАЛИНИН\r\n" +
                "Работу выполнили:\r\n" +
                "Михалев Юрий\r\n" +
                "Калинин Владислав\r\n" +
                "29.02.2020",
                "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ModelingClick(object sender, RoutedEventArgs e) {
            if (!this.isModelingWindowShowing) {
                this.modelingWindow = new ModelingWindow(currentSignal);
                this.modelingWindow.Closed += (object sender, System.EventArgs e) => this.isModelingWindowShowing = false;
                this.modelingWindow.Show();
                this.isModelingWindowShowing = true;
            }
        }

        public void AddChannel(Channel channel) {
            channel.StartDateTime = this.currentSignal.StartDateTime;
            channel.SamplingFrq = this.currentSignal.SamplingFrq;

            this.currentSignal.channels.Add(channel);

            if (aboutSignalWindow != null) {
                aboutSignalWindow.UpdateInfo(this.currentSignal);
            }

            if (modelingWindow != null) {
                modelingWindow.Close();
            }

            var chart = new Chart(channel);
            chart.Height = 100;

            charts.Add(chart);
            channels.Children.Add(chart);

            chart.ContextMenu = new ContextMenu();

            var item1 = new MenuItem();
            item1.Header = "Осциллограмма";
            int cur = this.currentSignal.channels.Count - 1;
            item1.Click += (object sender, RoutedEventArgs args) => {
                OpenOscillograms();

                oscillogramWindow.AddChannel(currentSignal.channels[cur]);
            };

            chart.ContextMenu.Items.Add(item1);

            chart.Begin = 0;
            chart.End = currentSignal.SamplesCount;
            
        }

        public void ResetSignal(Signal newSignal) {
            if (aboutSignalWindow != null) {
                aboutSignalWindow.Close();
            }

            if (modelingWindow != null) {
                modelingWindow.Close();
            }

            if (isOscillogramShowing) {
                oscillogramWindow.Close();
            }

            foreach (var chart in charts) {
                channels.Children.Remove(chart);
            }
            charts.Clear();

            SignalProcessing.Modelling.ResetCounters();

            this.currentSignal = newSignal;

            if (this.currentSignal == null) {
                return;
            }

            for (int i = 0; i < currentSignal.channels.Count; i++) {
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

                chart.Begin = 0;
                chart.End = currentSignal.SamplesCount;
            }
        }

        private void OpenFileClick(object sender, RoutedEventArgs e) {
            var openFileDialog = new OpenFileDialog();
            
            if (openFileDialog.ShowDialog() == true) {
                ResetSignal(Parser.Parse(openFileDialog.FileName));
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

            if (currentSignal == null || row >= currentSignal.channels.Count) {
                return;
            }

            foreach (var chart in charts) {
                chart.Selected = false;
                chart.InvalidateVisual();
            }

            activeChannelInGrid = charts[row];
            charts[row].Selected = true;
            //charts[row].SetSelectInterval((int)sliderBegin.Value, (int)sliderEnd.Value);
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

        private void SaveAs_Click(object sender, RoutedEventArgs e) {

        }
    }
}
