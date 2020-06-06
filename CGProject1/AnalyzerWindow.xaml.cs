using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CGProject1.SignalProcessing;

namespace CGProject1 {
    public partial class AnalyzerWindow : Window {

        private List<Analyzer> analyzers = new List<Analyzer>();
        private HashSet<string> namesSet = new HashSet<string>();

        private List<List<Chart>> charts = new List<List<Chart>>();
        private int begin;
        private int end;

        private int halfWindowSmoothing = 0;

        private int samplesCount = 0;

        private bool locked = false;
        private bool initilized = false;

        public AnalyzerWindow(int begin, int end) {
            InitializeComponent();

            for (int i = 0; i < ComboBoxMode.Items.Count; i++) charts.Add(new List<Chart>());

            BeginSelector.Text = begin.ToString();
            EndSelector.Text = end.ToString();

            this.begin = begin;
            this.end = end;
        }

        public void AddChannel(Channel channel) {
            if (namesSet.Contains(channel.Name)) {
                return;
            }

            namesSet.Add(channel.Name);
            var analyzer = new Analyzer(channel);

            analyzer.SetupChannel(begin, end);

            samplesCount = analyzer.SamplesCount;

            if (!initilized) {
                initilized = true;
                samplesCount = analyzer.AmplitudeSpectre().SamplesCount - 1;

                BeginSlider.Maximum = samplesCount - 1;
                EndSlider.Maximum = samplesCount - 1;

                BeginSlider.Value = 0;
                EndSlider.Value = samplesCount - 1;

                BeginBox.Text = 0.ToString();
                EndBox.Text = (samplesCount - 1).ToString();

                OscillogramScroll.Minimum = 0;
                OscillogramScroll.Maximum = samplesCount;
                double p = 0.999;
                OscillogramScroll.ViewportSize = samplesCount * p / (1.0 - p);
            }

            analyzers.Add(analyzer);
            SetupCharts(analyzer);

            UpdatePanel();
        }

        private void UpdatePanel()
        {
            if (SpectrePanel != null) SpectrePanel.Children.Clear();
            
            if (ComboBoxMode.SelectedIndex >= 0 && ComboBoxMode.SelectedIndex < charts.Count)
            {
                for (int i = 0; i < charts[ComboBoxMode.SelectedIndex].Count; i++)
                {
                    var item = charts[ComboBoxMode.SelectedIndex][i];
                    item.DisplayHAxisTitle = false;
                    item.DisplayHAxisInfo = false;
                    item.HAxisAlligment = Chart.HAxisAlligmentEnum.Top;

                    if (i == 0 || i + 1 == charts[ComboBoxMode.SelectedIndex].Count)
                    {
                        item.DisplayHAxisInfo = true;
                        item.DisplayHAxisTitle = true;
                    }
                    if (i + 1 == charts[ComboBoxMode.SelectedIndex].Count)
                    {
                        item.HAxisAlligment = Chart.HAxisAlligmentEnum.Bottom;
                    }

                    SpectrePanel.Children.Add(item);
                }
            }
        }

        private void UpdateAnalyzers() {
            foreach(var chartList in charts) {
                chartList.Clear();
            }

            foreach (var analyzer in analyzers) {
                analyzer.HalfWindowSmoothing = this.halfWindowSmoothing;
                analyzer.SetupChannel(this.begin, this.end);

                SetupCharts(analyzer);
            }

            UpdatePanel();
            locked = false;
            if (BeginSlider != null && EndSlider != null) {
                InputBeginEnd((int)BeginSlider.Value, (int)EndSlider.Value);
            }
            
        }

        private void SetupCharts(Analyzer analyzer) {
            Channel amp = analyzer.AmplitudeSpectre();

            var ampChart = new Chart(amp);
            ampChart.Height = 200;
            ampChart.Begin = 0;
            ampChart.End = amp.SamplesCount;
            if (charts[1].Count != 0)
            {
                ampChart.Begin = charts[1][0].Begin;
                ampChart.End = charts[1][0].End;
            }
            ampChart.Margin = new Thickness(0, 2, 0, 2);
            ampChart.GridDraw = true;
            ampChart.HAxisTitle = "Частота (Гц)";
            ampChart.MappingXAxis = MappingXAxis;
            ampChart.MaxHeightXAxisString = double.MaxValue.ToString();
            ampChart.ShowCurrentXY = true;
            ampChart.IsMouseSelect = true;
            ampChart.OnMouseSelect += OnMouseSelect;
            ampChart.Scaling = Chart.ScalingMode.LocalZeroed;
            charts[1].Add(ampChart);

            var psd = analyzer.PowerSpectralDensity();
            var psdChart = new Chart(psd);
            psdChart.Height = 200;
            psdChart.Begin = 0;
            psdChart.End = psd.SamplesCount;
            if (charts[0].Count != 0)
            {
                psdChart.Begin = charts[0][0].Begin;
                psdChart.End = charts[0][0].End;
            }
            psdChart.Margin = new Thickness(0, 2, 0, 2);
            psdChart.GridDraw = true;
            psdChart.HAxisTitle = "Частота (Гц)";
            psdChart.MappingXAxis = MappingXAxis;
            psdChart.MaxHeightXAxisString = double.MaxValue.ToString();
            psdChart.ShowCurrentXY = true;
            psdChart.IsMouseSelect = true;
            psdChart.OnMouseSelect += OnMouseSelect;
            psdChart.Scaling = Chart.ScalingMode.LocalZeroed;
            charts[0].Add(psdChart);

            var lgPSD = analyzer.LogarithmicPSD();
            var logPSDChart = new Chart(lgPSD);
            logPSDChart.Height = 200;
            logPSDChart.Begin = 0;
            logPSDChart.End = lgPSD.SamplesCount;
            if (charts[2].Count != 0)
            {
                logPSDChart.Begin = charts[2][0].Begin;
                logPSDChart.End = charts[2][0].End;
            }
            logPSDChart.Margin = new Thickness(0, 2, 0, 2);
            logPSDChart.GridDraw = true;
            logPSDChart.HAxisTitle = "Частота (Гц)";
            logPSDChart.MappingXAxis = MappingXAxis;
            logPSDChart.MaxHeightXAxisString = double.MaxValue.ToString();
            logPSDChart.ShowCurrentXY = true;
            logPSDChart.IsMouseSelect = true;
            logPSDChart.OnMouseSelect += OnMouseSelect;
            logPSDChart.Scaling = Chart.ScalingMode.Local;
            charts[2].Add(logPSDChart);

            var lg = analyzer.LogarithmicSpectre();
            var logChart = new Chart(lg);
            logChart.Height = 200;
            logChart.Begin = 0;
            logChart.End = lg.SamplesCount;
            if (charts[3].Count != 0)
            {
                logChart.Begin = charts[3][0].Begin;
                logChart.End = charts[3][0].End;
            }
            logChart.Margin = new Thickness(0, 2, 0, 2);
            logChart.GridDraw = true;
            logChart.HAxisTitle = "Частота (Гц)";
            logChart.MappingXAxis = MappingXAxis;
            logChart.MaxHeightXAxisString = double.MaxValue.ToString();
            logChart.ShowCurrentXY = true;
            logChart.IsMouseSelect = true;
            logChart.OnMouseSelect += OnMouseSelect;
            logChart.Scaling = Chart.ScalingMode.Local;
            charts[3].Add(logChart);
        }

        private string MappingXAxis(int idx, Chart chart)
        {
            double curVal = chart.Channel.DeltaTime * idx;
            return curVal.ToString("N6", CultureInfo.InvariantCulture);
        }

        private void OnMouseSelect(Chart sender, int newBegin, int newEnd)
        {
            for (int i = 0; i < charts.Count; i++)
            {
                foreach(var chart in charts[i])
                {
                    if (chart != sender)
                    {
                        chart.Begin = sender.Begin;
                        chart.End = sender.End;
                    }
                }
            }
        }

        private void ComboBoxMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdatePanel();
        }

        private void SelectInterval(object sender, RoutedEventArgs e) {
            int newBegin = this.begin;
            int newEnd = this.end;
            int newL = this.halfWindowSmoothing;
            if (!int.TryParse(HalfWindowTextBox.Text, out newL) || !int.TryParse(BeginSelector.Text, out newBegin) || !int.TryParse(EndSelector.Text, out newEnd)) {
                MessageBox.Show("Некорректные параметры", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (newEnd <= newBegin + 1) {
                MessageBox.Show("Некорректные параметры", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            HalfWindowTextBox.Text = newL.ToString();
            BeginSelector.Text = newBegin.ToString();
            EndSelector.Text = newEnd.ToString();

            this.begin = newBegin;
            this.end = newEnd;
            this.halfWindowSmoothing = newL;

            UpdateAnalyzers();
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

        private void ZeroMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            if (ZeroModeSelector.SelectedIndex >= 0 && ZeroModeSelector.SelectedIndex < 3) {
                foreach (var analyzer in analyzers) {
                    switch (ZeroModeSelector.SelectedIndex) {
                        case 0:
                            analyzer.zeroMode = Analyzer.ZeroMode.Nothing;
                            break;
                        case 1:
                            analyzer.zeroMode = Analyzer.ZeroMode.Null;
                            break;
                        case 2:
                            analyzer.zeroMode = Analyzer.ZeroMode.Smooth;
                            break;
                    }
                }

                UpdateAnalyzers();
            }
        }

        private bool TextIsNumeric(string input)
        {
            return input.All(c => char.IsDigit(c) || char.IsControl(c));
        }

        private void OscillogramScroll_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
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

        private void BeginSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            InputBeginEnd((int)e.NewValue, (int)EndSlider.Value);
        }

        private void EndSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            InputBeginEnd((int)BeginSlider.Value, (int)e.NewValue);
        }

        private void textBox_ValueChanged(object sender, EventArgs e)
        {
            long begin;
            long end;

            if (!long.TryParse(BeginBox.Text, out begin))
            {
                begin = 0;
            }

            if (!long.TryParse(EndBox.Text, out end))
            {
                end = samplesCount;
            }

            InputBeginEnd(begin, end);
        }

        private void InputBeginEnd(long begin, long end)
        {
            if (locked)
            {
                locked = false;
                return;
            }

            if (begin > samplesCount)
            {
                begin = samplesCount;
            }

            if (begin < 0)
            {
                begin = 0;
            }

            if (end > samplesCount)
            {
                end = samplesCount;
            }

            if (end < begin)
            {
                end = begin + 1;
            }

            double p = (end - begin + 1) * 1.0 / samplesCount;
            if (Math.Abs(p - 1.0) < 1e-5) p = 0.999;
            OscillogramScroll.ViewportSize = samplesCount * p / Math.Abs(1.0 - p);
            locked = true;
            OscillogramScroll.Value = begin * (1.0 + p / Math.Abs(1.0 - p));

            for (int i = 0; i < charts.Count; i++)
            {
                foreach (var chart in charts[i])
                {
                    chart.Begin = (int)begin;
                    chart.End = (int)end;
                }
            }

            BeginSlider.Value = begin;
            EndSlider.Value = end;
            BeginBox.Text = begin.ToString();
            EndBox.Text = end.ToString();
            if (charts.Count != 0 && charts[0].Count != 0)
            {
                BeginFrequencyLabel.Content = "Begin frequency: " + MappingXAxis(charts[0][0].Begin, charts[0][0]);
                EndFrequencyLabel.Content = "End frequency: " + MappingXAxis(charts[0][0].End, charts[0][0]);
            }
        }
    }
}
