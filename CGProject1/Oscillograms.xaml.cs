using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;

namespace CGProject1 {
    /// <summary>
    /// Interaction logic for Oscillograms.xaml
    /// </summary>
    public partial class Oscillograms : Window {
        private HashSet<Chart> activeCharts;

        private int samplesCount = 0;

        bool locked = false;
        DateTime startTime;
        double deltaTime;

        public Oscillograms() {
            InitializeComponent();
            activeCharts = new HashSet<Chart>();

            var globalScaling = new MenuItem();
            globalScaling.Header = "Глобальное";
            globalScaling.Click += GlobalScaling_Click;
            ScalingChooser.Items.Add(globalScaling);

            var autoScaling = new MenuItem();
            autoScaling.Header = "Локальное";
            autoScaling.Click += AutoScaling_Click;
            ScalingChooser.Items.Add(autoScaling);

            var fixedScaling = new MenuItem();
            fixedScaling.Header = "Фиксированное";
            fixedScaling.Click += FixedScaling_Click;
            ScalingChooser.Items.Add(fixedScaling);

            var uniformGlobalScaling = new MenuItem();
            uniformGlobalScaling.Header = "Единое глобальное";
            uniformGlobalScaling.Click += UniformGlobalScaling_Click;
            ScalingChooser.Items.Add(uniformGlobalScaling);

            var uniformLocalScaling = new MenuItem();
            uniformLocalScaling.Header = "Единое локальное";
            uniformLocalScaling.Click += UniformLocalScaling_Click;
            ScalingChooser.Items.Add(uniformLocalScaling);
        }

        public void Update(Signal signal) {
            BeginSlider.IsEnabled = signal != null;
            EndSlider.IsEnabled = signal != null;
            BeginBox.IsEnabled = signal != null;
            EndBox.IsEnabled = signal != null;
            OscillogramScroll.IsEnabled = signal != null;

            //ResetScalingMode(defaultScaling);
            
            if (signal != null) {
                samplesCount = signal.SamplesCount;
                OscillogramsField.Children.Clear();
                activeCharts.Clear();

                BeginSlider.Maximum = samplesCount;
                EndSlider.Maximum = samplesCount;

                BeginSlider.Value = 0;
                EndSlider.Value = samplesCount;

                BeginBox.Text = 0.ToString();
                EndBox.Text = samplesCount.ToString();

                OscillogramScroll.Minimum = 0;
                OscillogramScroll.Maximum = signal.SamplesCount;
                double p = 0.999;
                OscillogramScroll.ViewportSize = signal.SamplesCount * p / (1.0 - p);

                startTime = signal.StartDateTime;
                deltaTime = signal.DeltaTime;
            }
            
        }

        public void AddChannel(Channel channel) {
            var newChart = new Chart(channel);

            newChart.Begin = (int)BeginSlider.Value;
            newChart.End = (int)EndSlider.Value;

            newChart.Height = 300;
            newChart.Margin = new Thickness(0, 2, 0, 2);

            OscillogramsField.Children.Add(newChart);
            newChart.GridDraw = true;
            activeCharts.Add(newChart);

            newChart.ContextMenu = new ContextMenu();
            var item1 = new MenuItem();
            item1.Header = "Закрыть канал";
            item1.Click += (object sender, RoutedEventArgs args) => {
                OscillogramsField.Children.Remove(newChart);
                activeCharts.Remove(newChart);
            };

            newChart.ContextMenu.Items.Add(item1);

            var scaleMenu = new MenuItem();
            scaleMenu.Header = "Масштабирование";
            newChart.ContextMenu.Items.Add(scaleMenu);

            #region Scale
            var globalScaling = new MenuItem();
            globalScaling.Header = "Глобальное";
            globalScaling.Click += (object sender, RoutedEventArgs args) => {
                newChart.Scaling = Chart.ScalingMode.Global;
            };
            scaleMenu.Items.Add(globalScaling);

            var autoScaling = new MenuItem();
            autoScaling.Header = "Локальное";
            autoScaling.Click += (object sender, RoutedEventArgs args) => {
                newChart.Scaling = Chart.ScalingMode.Local;
            };
            scaleMenu.Items.Add(autoScaling);

            var fixedScaling = new MenuItem();
            fixedScaling.Header = "Фиксированное";
            fixedScaling.Click += (object sender, RoutedEventArgs args) => {
                var settings = new SettingsFixedScale();
                settings.ShowDialog();
                if (settings.Status)
                {
                    newChart.Scaling = Chart.ScalingMode.Fixed;
                    newChart.MinFixedScale = settings.From;
                    newChart.MaxFixedScale = settings.To;
                }
            };
            scaleMenu.Items.Add(fixedScaling);

            var uniformGlobalScaling = new MenuItem();
            uniformGlobalScaling.Header = "Единое глобальное";
            uniformGlobalScaling.Click += (object sender, RoutedEventArgs args) => {
                newChart.GroupedCharts = activeCharts.ToList();
                newChart.Scaling = Chart.ScalingMode.UniformGlobal;
            };
            scaleMenu.Items.Add(uniformGlobalScaling);

            var uniformLocalScaling = new MenuItem();
            uniformLocalScaling.Header = "Единое локальное";
            uniformLocalScaling.Click += (object sender, RoutedEventArgs args) => {
                newChart.GroupedCharts = activeCharts.ToList();
                newChart.Scaling = Chart.ScalingMode.UniformLocal;
            };
            scaleMenu.Items.Add(uniformLocalScaling);
            #endregion Scale
        }

        private void BeginSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            InputBeginEnd((int)e.NewValue, (int)EndSlider.Value);
        }

        private void EndSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            InputBeginEnd((int)BeginSlider.Value, (int)e.NewValue);
        }

        private void textBox_ValueChanged(object sender, EventArgs e) {
            long begin;
            long end;

            if (!long.TryParse(BeginBox.Text, out begin)) {
                begin = 0;
            }

            if (!long.TryParse(EndBox.Text, out end)) {
                end = samplesCount;
            }

            InputBeginEnd(begin, end);
        }

        private void InputBeginEnd(long begin, long end) {
            if (locked)
            {
                locked = false;
                return;
            }

            if (begin > samplesCount) {
                begin = samplesCount;
            }

            if (begin < 0) {
                begin = 0;
            }

            if (end > samplesCount) {
                end = samplesCount;
            }

            if (end < begin) {
                end = begin;
            }

            double p = (end - begin + 1) * 1.0 / samplesCount;
            if (Math.Abs(p - 1.0) < 1e-5) p = 0.999;
            OscillogramScroll.ViewportSize = samplesCount * p / Math.Abs(1.0 - p);
            locked = true;
            OscillogramScroll.Value = begin * (1.0 + p / Math.Abs(1.0 - p));
            

            foreach (var chart in activeCharts) {
                chart.Begin = (int)begin;
                chart.End = (int)end;
            }

            BeginSlider.Value = begin;
            EndSlider.Value = end;
            BeginBox.Text = begin.ToString();
            EndBox.Text = end.ToString();
            BeginTimeLabel.Content = (startTime + TimeSpan.FromSeconds(deltaTime * begin)).ToString("dd-MM-yyyy hh\\:mm\\:ss");
            EndTimeLabel.Content = (startTime + TimeSpan.FromSeconds(deltaTime * end)).ToString("dd-MM-yyyy hh\\:mm\\:ss");
        }

        private void previewTextInput(object sender, TextCompositionEventArgs e) {
            e.Handled = !TextIsNumeric(e.Text);
        }

        private void previewPasting(object sender, DataObjectPastingEventArgs e) {
            if (e.DataObject.GetDataPresent(typeof(string))) {
                string input = (string)e.DataObject.GetData(typeof(string));
                if (!TextIsNumeric(input)) e.CancelCommand();
            } else {
                e.CancelCommand();
            }
        }

        private bool TextIsNumeric(string input) {
            return input.All(c => char.IsDigit(c) || char.IsControl(c));
        }

        private void OscillogramScroll_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (locked)
            {
                locked = false;
                return;
            }

            double p = (EndSlider.Value - BeginSlider.Value + 1) * 1.0 / samplesCount;
            double begin = e.NewValue * Math.Abs(1.0 - p);
            double end = begin + EndSlider.Value - BeginSlider.Value;
            locked = true;
            BeginSlider.Value = begin;
            EndSlider.Value = end;
        }

        //private void ResetScalingMode(MenuItem scalingMode) {
        //    foreach (MenuItem item in ScalingChooser.Items) {
        //        if (item == scalingMode) {
        //            item.IsChecked = true;
        //        } else {
        //            item.IsChecked = false;
        //        }
        //    }
        //}

        private void GlobalScaling_Click(object sender, RoutedEventArgs e) {
            //ResetScalingMode(sender as MenuItem);

            foreach (var chart in activeCharts) {
                chart.Scaling = Chart.ScalingMode.Global;
            }
        }

        private void AutoScaling_Click(object sender, RoutedEventArgs e) {
            //ResetScalingMode(sender as MenuItem);

            foreach (var chart in activeCharts) {
                chart.Scaling = Chart.ScalingMode.Local;
            }
        }
        private void FixedScaling_Click(object sender, RoutedEventArgs e) {
            //ResetScalingMode(sender as MenuItem);

            var settings = new SettingsFixedScale();
            settings.ShowDialog();
            if (settings.Status)
            {
                foreach (var chart in activeCharts)
                {
                    chart.Scaling = Chart.ScalingMode.Fixed;
                    chart.MinFixedScale = settings.From;
                    chart.MaxFixedScale = settings.To;
                }
            }
        }
        private void UniformLocalScaling_Click(object sender, RoutedEventArgs e)
        {
            //ResetScalingMode(sender as MenuItem);

            foreach (var chart in activeCharts)
            {
                chart.GroupedCharts = activeCharts.ToList();
                chart.Scaling = Chart.ScalingMode.UniformLocal;
            }
        }
        private void UniformGlobalScaling_Click(object sender, RoutedEventArgs e)
        {
            //ResetScalingMode(sender as MenuItem);

            foreach (var chart in activeCharts)
            {
                chart.GroupedCharts = activeCharts.ToList();
                chart.Scaling = Chart.ScalingMode.UniformGlobal;
            }
        }
    }
}
