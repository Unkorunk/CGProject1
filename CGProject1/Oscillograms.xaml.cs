using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;
using CGProject1.Chart;

namespace CGProject1
{
    /// <summary>
    /// Interaction logic for Oscillograms.xaml
    /// </summary>
    public partial class Oscillograms : Window
    {
        private readonly HashSet<ChartLine> activeCharts = new HashSet<ChartLine>();

        private int samplesCount = 0;
        private DateTime startTime;
        private double deltaTime;

        public Oscillograms()
        {
            InitializeComponent();

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

        public int GetBegin() => FSelector.LeftSlider;

        public int GetEnd() => FSelector.RightSlider;

        public void Update(Signal signal)
        {
            FSelector.IsEnabled = signal != null;
            BeginBox.IsEnabled = signal != null;
            EndBox.IsEnabled = signal != null;

            if (signal != null)
            {
                samplesCount = signal.SamplesCount;
                OscillogramsField.Children.Clear();
                activeCharts.Clear();

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
        }

        public void AddChannel(Channel channel)
        {
            var newChart = new ChartLine(channel)
            {
                IsMouseSelect = true,
                ShowCurrentXY = true
            };

            newChart.Begin = GetBegin();
            newChart.End = GetEnd();

            newChart.OnMouseSelect += (sender, newBegin, newEnd) =>
            {
                FSelector.LeftSlider = newBegin;
                FSelector.RightSlider = newEnd;
            };

            newChart.Height = 300;
            newChart.Margin = new Thickness(0, 2, 0, 2);

            OscillogramsField.Children.Add(newChart);
            newChart.GridDraw = true;
            activeCharts.Add(newChart);

            newChart.ContextMenu = new ContextMenu();
            var item1 = new MenuItem();
            item1.Header = "Закрыть канал";
            item1.Click += (object sender, RoutedEventArgs args) =>
            {
                OscillogramsField.Children.Remove(newChart);
                activeCharts.Remove(newChart);
            };

            newChart.ContextMenu.Items.Add(item1);

            var scaleMenu = new MenuItem();
            scaleMenu.Header = "Масштабирование";
            newChart.ContextMenu.Items.Add(scaleMenu);

            var globalScaling = new MenuItem();
            globalScaling.Header = "Глобальное";
            globalScaling.Click += (object sender, RoutedEventArgs args) =>
            {
                newChart.Scaling = ChartLine.ScalingMode.Global;
            };
            scaleMenu.Items.Add(globalScaling);

            var autoScaling = new MenuItem();
            autoScaling.Header = "Локальное";
            autoScaling.Click += (object sender, RoutedEventArgs args) =>
            {
                newChart.Scaling = ChartLine.ScalingMode.Local;
            };
            scaleMenu.Items.Add(autoScaling);

            var fixedScaling = new MenuItem();
            fixedScaling.Header = "Фиксированное";
            fixedScaling.Click += (object sender, RoutedEventArgs args) =>
            {
                var settings = new SettingsFixedScale();
                settings.ShowDialog();
                if (settings.Status)
                {
                    newChart.Scaling = ChartLine.ScalingMode.Fixed;
                    newChart.MinFixedScale = settings.From;
                    newChart.MaxFixedScale = settings.To;
                }
            };
            scaleMenu.Items.Add(fixedScaling);

            var uniformGlobalScaling = new MenuItem();
            uniformGlobalScaling.Header = "Единое глобальное";
            uniformGlobalScaling.Click += (object sender, RoutedEventArgs args) =>
            {
                newChart.GroupedCharts = activeCharts.ToList();
                newChart.Scaling = ChartLine.ScalingMode.UniformGlobal;
            };
            scaleMenu.Items.Add(uniformGlobalScaling);

            var uniformLocalScaling = new MenuItem();
            uniformLocalScaling.Header = "Единое локальное";
            uniformLocalScaling.Click += (object sender, RoutedEventArgs args) =>
            {
                newChart.GroupedCharts = activeCharts.ToList();
                newChart.Scaling = ChartLine.ScalingMode.UniformLocal;
            };
            scaleMenu.Items.Add(uniformLocalScaling);

            var statisticsMenuItem = new MenuItem();
            statisticsMenuItem.Header = "Статистика";
            statisticsMenuItem.Click += (object sender, RoutedEventArgs e) =>
            {
                if (!MainWindow.instance.isStatisticShowing)
                {
                    MainWindow.instance.statisticsWindow = new StatisticsWindow();
                    MainWindow.instance.isStatisticShowing = true;
                    MainWindow.instance.statisticsWindow.Closed += (object sender, EventArgs e) => MainWindow.instance.isStatisticShowing = false;
                    MainWindow.instance.statisticsWindow.Show();
                }

                MainWindow.instance.statisticsWindow.Update(newChart, false);
            };
            newChart.ContextMenu.Items.Add(statisticsMenuItem);

            if (MainWindow.instance.isStatisticShowing)
            {
                MainWindow.instance.statisticsWindow.ReplaceChart(newChart);
            }

            InputBeginEnd(GetBegin(), GetEnd());
        }

        private void textBox_ValueChanged(object sender, EventArgs e)
        {
            if (!int.TryParse(BeginBox.Text, out int begin))
            {
                begin = 0;
            }

            if (!int.TryParse(EndBox.Text, out int end))
            {
                end = samplesCount;
            }

            InputBeginEnd(begin, end);
        }

        private void InputBeginEnd(int begin, int end)
        {
            end = Math.Clamp(end, 0, samplesCount - 1);
            begin = Math.Clamp(begin, 0, end - 1);

            foreach (var chart in activeCharts)
            {
                chart.Begin = begin;
                chart.End = end;
            }

            if (GetBegin() != begin) FSelector.LeftSlider = begin;
            if (GetEnd() != end) FSelector.RightSlider = end;
            if (BeginBox.Text != begin.ToString()) BeginBox.Text = begin.ToString();
            if (EndBox.Text != end.ToString()) EndBox.Text = end.ToString();

            BeginTimeLabel.Content = "Start Time: " + (startTime + TimeSpan.FromSeconds(deltaTime * begin)).ToString("dd-MM-yyyy hh\\:mm\\:ss");
            EndTimeLabel.Content = "End Time: " + (startTime + TimeSpan.FromSeconds(deltaTime * end)).ToString("dd-MM-yyyy hh\\:mm\\:ss");
        }

        private void previewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !TextIsNumeric(e.Text);
        }

        private void previewPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string input = (string)e.DataObject.GetData(typeof(string));
                if (!TextIsNumeric(input)) e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private bool TextIsNumeric(string input)
        {
            return input.All(c => char.IsDigit(c) || char.IsControl(c));
        }

        private void GlobalScaling_Click(object sender, RoutedEventArgs e)
        {
            foreach (var chart in activeCharts)
            {
                chart.Scaling = ChartLine.ScalingMode.Global;
            }
        }

        private void AutoScaling_Click(object sender, RoutedEventArgs e)
        {
            foreach (var chart in activeCharts)
            {
                chart.Scaling = ChartLine.ScalingMode.Local;
            }
        }

        private void FixedScaling_Click(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsFixedScale();
            settings.ShowDialog();

            if (settings.Status)
            {
                foreach (var chart in activeCharts)
                {
                    chart.Scaling = ChartLine.ScalingMode.Fixed;
                    chart.MinFixedScale = settings.From;
                    chart.MaxFixedScale = settings.To;
                }
            }
        }

        private void UniformLocalScaling_Click(object sender, RoutedEventArgs e)
        {
            foreach (var chart in activeCharts)
            {
                chart.GroupedCharts = activeCharts.ToList();
                chart.Scaling = ChartLine.ScalingMode.UniformLocal;
            }
        }

        private void UniformGlobalScaling_Click(object sender, RoutedEventArgs e)
        {
            foreach (var chart in activeCharts)
            {
                chart.GroupedCharts = activeCharts.ToList();
                chart.Scaling = ChartLine.ScalingMode.UniformGlobal;
            }
        }

        private void FSelector_IntervalUpdate(object sender, EventArgs e)
        {
            InputBeginEnd(FSelector.LeftSlider, FSelector.RightSlider);
        }
    }
}
