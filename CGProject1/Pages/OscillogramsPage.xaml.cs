using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using CGProject1.Chart;
using CGProject1.Pages;

namespace CGProject1 {
    public partial class OscillogramsPage : Page, IChannelComponent {
        private readonly HashSet<ChartLine> activeCharts = new HashSet<ChartLine>();
        private readonly HashSet<string> activeChartsNames = new HashSet<string>();

        private int samplesCount = 0;
        private DateTime startTime;
        private double deltaTime;

        private double chartHeight = 100;

        private bool isReset = false;

        public OscillogramsPage() {
            InitializeComponent();

            CountPerPage.Minimum = 1;
            CountPerPage.Maximum = 6;
            CountPerPage.Value = 1;

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

            RecalculateHeight(1);
        }

        public int GetBegin() => FSelector.LeftSlider;

        public int GetEnd() => FSelector.RightSlider;

        public void UpdateActiveSegment(int begin, int end) { }

        public void Reset(Signal signal) {
            isReset = true;

            FSelector.IsEnabled = signal != null;
            BeginBox.IsEnabled = signal != null;
            EndBox.IsEnabled = signal != null;

            if (signal != null) {
                samplesCount = signal.SamplesCount;
                OscillogramsField.Children.Clear();
                activeCharts.Clear();
                activeChartsNames.Clear();

                FSelector.Minimum = 0;
                FSelector.Maximum = samplesCount - 1;

                FSelector.LeftSlider = 0;
                FSelector.RightSlider = samplesCount - 1;

                BeginBox.Text = GetBegin().ToString();
                EndBox.Text = GetEnd().ToString();

                startTime = signal.StartDateTime;
                deltaTime = signal.DeltaTime;

                InputBeginEnd(GetBegin(), GetEnd());
            }

            isReset = false;
        }

        public void AddChannel(Channel channel) {
            if (activeChartsNames.Contains(channel.Name)) {
                return;
            }

            var newChart = new ChartLine(channel) {
                IsMouseSelect = true,
                ShowCurrentXY = true
            };

            newChart.Begin = GetBegin();
            newChart.End = GetEnd();

            newChart.OnMouseSelect += (sender, newBegin, newEnd) => {
                FSelector.LeftSlider = newBegin;
                FSelector.RightSlider = newEnd;
            };

            newChart.Height = this.chartHeight;
            newChart.Margin = new Thickness(0, 2, 0, 2);

            OscillogramsField.Children.Add(newChart);
            newChart.GridDraw = true;
            activeCharts.Add(newChart);
            activeChartsNames.Add(channel.Name);

            newChart.ContextMenu = new ContextMenu();
            var item1 = new MenuItem();
            item1.Header = "Закрыть канал";
            item1.Click += (object sender, RoutedEventArgs args) => {
                OscillogramsField.Children.Remove(newChart);
                activeCharts.Remove(newChart);
                activeChartsNames.Remove(channel.Name);
            };

            newChart.ContextMenu.Items.Add(item1);

            var scaleMenu = new MenuItem();
            scaleMenu.Header = "Масштабирование";
            newChart.ContextMenu.Items.Add(scaleMenu);

            var globalScaling = new MenuItem();
            globalScaling.Header = "Глобальное";
            globalScaling.Click += (object sender, RoutedEventArgs args) => {
                newChart.Scaling = ChartLine.ScalingMode.Global;
            };
            scaleMenu.Items.Add(globalScaling);

            var autoScaling = new MenuItem();
            autoScaling.Header = "Локальное";
            autoScaling.Click += (object sender, RoutedEventArgs args) => {
                newChart.Scaling = ChartLine.ScalingMode.Local;
            };
            scaleMenu.Items.Add(autoScaling);

            var fixedScaling = new MenuItem();
            fixedScaling.Header = "Фиксированное";
            fixedScaling.Click += (object sender, RoutedEventArgs args) => {
                var settings = new SettingsFixedScale();
                settings.ShowDialog();
                if (settings.Status) {
                    newChart.Scaling = ChartLine.ScalingMode.Fixed;
                    newChart.MinFixedScale = settings.From;
                    newChart.MaxFixedScale = settings.To;
                }
            };
            scaleMenu.Items.Add(fixedScaling);

            var uniformGlobalScaling = new MenuItem();
            uniformGlobalScaling.Header = "Единое глобальное";
            uniformGlobalScaling.Click += (object sender, RoutedEventArgs args) => {
                newChart.GroupedCharts = activeCharts.ToList();
                newChart.Scaling = ChartLine.ScalingMode.UniformGlobal;
            };
            scaleMenu.Items.Add(uniformGlobalScaling);

            var uniformLocalScaling = new MenuItem();
            uniformLocalScaling.Header = "Единое локальное";
            uniformLocalScaling.Click += (object sender, RoutedEventArgs args) => {
                newChart.GroupedCharts = activeCharts.ToList();
                newChart.Scaling = ChartLine.ScalingMode.UniformLocal;
            };
            scaleMenu.Items.Add(uniformLocalScaling);

            var statisticsMenuItem = new MenuItem();
            statisticsMenuItem.Header = "Статистика";
            statisticsMenuItem.Click += (object sender, RoutedEventArgs e) => {
                MainWindow.instance.AddStatistics(channel);
            };
            newChart.ContextMenu.Items.Add(statisticsMenuItem);

            InputBeginEnd(GetBegin(), GetEnd());
        }

        private void ResetSegmentClick(object sender, RoutedEventArgs e) {
            InputBeginEnd(0, samplesCount - 1);
        }

        private void textBox_ValueChanged(object sender, EventArgs e) {
            if (!int.TryParse(BeginBox.Text, out int begin)) {
                begin = 0;
            }

            if (!int.TryParse(EndBox.Text, out int end)) {
                end = samplesCount - 1;
            }

            InputBeginEnd(begin, end);
        }

        private void InputBeginEnd(int begin, int end) {
            end = Math.Clamp(end, 0, samplesCount - 1);
            begin = Math.Clamp(begin, 0, end - 1);

            foreach (var chart in activeCharts) {
                chart.Begin = begin;
                chart.End = end;
            }

            if (GetBegin() != begin) FSelector.LeftSlider = begin;
            if (GetEnd() != end) FSelector.RightSlider = end;
            if (BeginBox.Text != begin.ToString()) BeginBox.Text = begin.ToString();
            if (EndBox.Text != end.ToString()) EndBox.Text = end.ToString();

            BeginTimeLabel.Content = "Start Time: " + (startTime + TimeSpan.FromSeconds(deltaTime * begin)).ToString("dd-MM-yyyy hh\\:mm\\:ss");
            EndTimeLabel.Content = "End Time: " + (startTime + TimeSpan.FromSeconds(deltaTime * end)).ToString("dd-MM-yyyy hh\\:mm\\:ss");

            if (!isReset) {
                MainWindow.instance.UpdateActiveSegment(begin, end);
            }
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

        private void GlobalScaling_Click(object sender, RoutedEventArgs e) {
            foreach (var chart in activeCharts) {
                chart.Scaling = ChartLine.ScalingMode.Global;
            }
        }

        private void AutoScaling_Click(object sender, RoutedEventArgs e) {
            foreach (var chart in activeCharts) {
                chart.Scaling = ChartLine.ScalingMode.Local;
            }
        }

        private void FixedScaling_Click(object sender, RoutedEventArgs e) {
            var settings = new SettingsFixedScale();
            settings.ShowDialog();

            if (settings.Status) {
                foreach (var chart in activeCharts) {
                    chart.Scaling = ChartLine.ScalingMode.Fixed;
                    chart.MinFixedScale = settings.From;
                    chart.MaxFixedScale = settings.To;
                }
            }
        }

        private void UniformLocalScaling_Click(object sender, RoutedEventArgs e) {
            foreach (var chart in activeCharts) {
                chart.GroupedCharts = activeCharts.ToList();
                chart.Scaling = ChartLine.ScalingMode.UniformLocal;
            }
        }

        private void UniformGlobalScaling_Click(object sender, RoutedEventArgs e) {
            foreach (var chart in activeCharts) {
                chart.GroupedCharts = activeCharts.ToList();
                chart.Scaling = ChartLine.ScalingMode.UniformGlobal;
            }
        }

        private void FSelector_IntervalUpdate(object sender, EventArgs e) {
            InputBeginEnd(FSelector.LeftSlider, FSelector.RightSlider);
        }

        private void RecalculateHeight(int count) {
            if (OscillogramScrollViewer.ActualHeight <= 0) {
                return;
            }

            double newHeight = (OscillogramScrollViewer.ActualHeight) / count;
            this.chartHeight = newHeight;

            foreach (var chart in activeCharts) {
                chart.Height = this.chartHeight;
            }
        }

        private void CountPerPage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            RecalculateHeight((int)e.NewValue);
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e) {
            RecalculateHeight((int)CountPerPage.Value);
        }
    }
}
