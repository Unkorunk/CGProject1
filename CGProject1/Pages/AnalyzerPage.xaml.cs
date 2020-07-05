using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CGProject1.Chart;
using CGProject1.SignalProcessing;


namespace CGProject1.Pages {
    public partial class AnalyzerPage : Page {
        private List<Analyzer> analyzers = new List<Analyzer>();
        private HashSet<string> namesSet = new HashSet<string>();

        private List<List<ChartLine>> charts = new List<List<ChartLine>>();
        private int begin;
        private int end;

        private int halfWindowSmoothing = 0;

        private int samplesCount = 0;

        private bool initiliezed = false;

        public AnalyzerPage() {
            InitializeComponent();

            for (int i = 0; i < ComboBoxMode.Items.Count; i++) charts.Add(new List<ChartLine>());

            if (MainWindow.instance.currentSignal != null) {
                BeginFrequencyLabel.Content = "Begin frequency: 0";
                EndFrequencyLabel.Content = $"End frequency: {(MainWindow.instance.currentSignal.SamplingFrq / 2).ToString(CultureInfo.InvariantCulture)}";
            }
        }

        public int GetBegin() => FSelector.LeftSlider;
        public int GetEnd() => FSelector.RightSlider;

        public void UpdateActiveSegment(int begin, int end) {
            BeginSelector.Text = begin.ToString();
            EndSelector.Text = end.ToString();

            this.begin = begin;
            this.end = end;

            initiliezed = false;

            UpdateAnalyzers();
        }

        public void Reset() {
            analyzers.Clear();
            namesSet.Clear();

            foreach (var item in charts) {
                item.Clear();
            }

            initiliezed = false;
            SpectrePanel.Children.Clear();
        }

        public void AddChannel(Channel channel) {
            if (namesSet.Contains(channel.Name)) {
                return;
            }

            namesSet.Add(channel.Name);
            var analyzer = new Analyzer(channel);

            analyzer.SetupChannel(begin, end);

            samplesCount = analyzer.SamplesCount;

            if (!initiliezed) {
                initiliezed = true;
                samplesCount = analyzer.AmplitudeSpectre().SamplesCount - 1;

                FSelector.Minimum = 0;
                FSelector.Maximum = samplesCount - 1;

                FSelector.LeftSlider = 0;
                FSelector.RightSlider = samplesCount - 1;

                BeginBox.Text = GetBegin().ToString();
                EndBox.Text = GetEnd().ToString();

                InputBeginEnd(GetBegin(), GetEnd());
            }

            analyzers.Add(analyzer);
            SetupCharts(analyzer);

            UpdatePanel();
        }

        private void UpdatePanel() {
            if (SpectrePanel != null) SpectrePanel.Children.Clear();

            if (ComboBoxMode.SelectedIndex >= 0 && ComboBoxMode.SelectedIndex < charts.Count) {
                for (int i = 0; i < charts[ComboBoxMode.SelectedIndex].Count; i++) {
                    var item = charts[ComboBoxMode.SelectedIndex][i];
                    item.DisplayHAxisTitle = false;
                    item.DisplayHAxisInfo = false;
                    item.HAxisAlligment = ChartLine.HAxisAlligmentEnum.Top;

                    if (i == 0 || i + 1 == charts[ComboBoxMode.SelectedIndex].Count) {
                        item.DisplayHAxisInfo = true;
                        item.DisplayHAxisTitle = true;
                    }
                    if (i + 1 == charts[ComboBoxMode.SelectedIndex].Count) {
                        item.HAxisAlligment = ChartLine.HAxisAlligmentEnum.Bottom;
                    }

                    SpectrePanel.Children.Add(item);
                }
            }
        }

        private void UpdateAnalyzers() {
            int b = 0, e = samplesCount - 1;
            if (FSelector != null) {
                b = GetBegin();
                e = GetEnd();
            }


            foreach (var chartList in charts) {
                chartList.Clear();
            }

            foreach (var analyzer in analyzers) {
                analyzer.HalfWindowSmoothing = this.halfWindowSmoothing;
                analyzer.SetupChannel(this.begin, this.end);

                if (!initiliezed) {
                    initiliezed = true;
                    samplesCount = analyzer.AmplitudeSpectre().SamplesCount - 1;

                    FSelector.Minimum = 0;
                    FSelector.LeftSlider = 0;

                    if (samplesCount < 1) {
                        FSelector.Maximum = 1;
                        FSelector.RightSlider = 1;
                    }


                    BeginBox.Text = GetBegin().ToString();
                    EndBox.Text = GetEnd().ToString();

                    InputBeginEnd(GetBegin(), GetEnd());
                }

                SetupCharts(analyzer);
            }

            UpdatePanel();
            if (FSelector != null) {
                if (b >= FSelector.Maximum) {
                    b = FSelector.Maximum - 1;
                }
                if (e > FSelector.Maximum) {
                    e = FSelector.Maximum;
                }

                InputBeginEnd(b, e);
            }
        }

        private void SetupCharts(Analyzer analyzer) {
            Channel amp = analyzer.AmplitudeSpectre();

            var menuItems = new MenuItem[4];
            for (int i = 0; i < 4; i++) {
                menuItems[i] = new MenuItem();
                menuItems[i].Header = "Закрыть канал";
                menuItems[i].Click += (object sender, RoutedEventArgs args) => {
                    analyzers.Remove(analyzer);
                    UpdateAnalyzers();
                };
            }
           

            var ampChart = new ChartLine(amp);
            ampChart.Height = 200;
            ampChart.Begin = 0;
            ampChart.End = amp.SamplesCount;
            if (charts[1].Count != 0) {
                ampChart.Begin = charts[1][0].Begin;
                ampChart.End = charts[1][0].End;
            }
            FrequencyChartSetup(ampChart);
            ampChart.Scaling = ChartLine.ScalingMode.LocalZeroed;
            ampChart.ContextMenu = new ContextMenu();
            ampChart.ContextMenu.Items.Add(menuItems[1]);
            charts[1].Add(ampChart);

            var psd = analyzer.PowerSpectralDensity();
            var psdChart = new ChartLine(psd);
            psdChart.Height = 200;
            psdChart.Begin = 0;
            psdChart.End = psd.SamplesCount;
            if (charts[0].Count != 0) {
                psdChart.Begin = charts[0][0].Begin;
                psdChart.End = charts[0][0].End;
            }
            FrequencyChartSetup(psdChart);
            psdChart.Scaling = ChartLine.ScalingMode.LocalZeroed;
            psdChart.ContextMenu = new ContextMenu();
            psdChart.ContextMenu.Items.Add(menuItems[0]);
            charts[0].Add(psdChart);

            var lgPSD = analyzer.LogarithmicPSD();
            var logPSDChart = new ChartLine(lgPSD);
            logPSDChart.Height = 200;
            logPSDChart.Begin = 0;
            logPSDChart.End = lgPSD.SamplesCount;
            if (charts[2].Count != 0) {
                logPSDChart.Begin = charts[2][0].Begin;
                logPSDChart.End = charts[2][0].End;
            }
            FrequencyChartSetup(logPSDChart);
            logPSDChart.Scaling = ChartLine.ScalingMode.Local;
            logPSDChart.ContextMenu = new ContextMenu();
            logPSDChart.ContextMenu.Items.Add(menuItems[2]);
            charts[2].Add(logPSDChart);

            var lg = analyzer.LogarithmicSpectre();
            var logChart = new ChartLine(lg);
            logChart.Height = 200;
            logChart.Begin = 0;
            logChart.End = lg.SamplesCount;
            if (charts[3].Count != 0) {
                logChart.Begin = charts[3][0].Begin;
                logChart.End = charts[3][0].End;
            }
            FrequencyChartSetup(logChart);
            logChart.Scaling = ChartLine.ScalingMode.Local;
            logChart.ContextMenu = new ContextMenu();
            logChart.ContextMenu.Items.Add(menuItems[3]);
            charts[3].Add(logChart);

            FSelector.Maximum = lg.SamplesCount - 1;
        }

        private string MappingXAxis(int idx, ChartLine chart) {
            double curVal = chart.Channel.DeltaTime * idx;
            return curVal.ToString("N6", CultureInfo.InvariantCulture);
        }

        private void OnMouseSelect(ChartLine sender, int newBegin, int newEnd) {
            for (int i = 0; i < charts.Count; i++) {
                foreach (var chart in charts[i]) {
                    if (chart != sender) {
                        chart.Begin = sender.Begin;
                        chart.End = sender.End;
                    }
                }
            }

            InputBeginEnd(sender.Begin, sender.End);
        }

        private void ComboBoxMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
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

        private bool TextIsNumeric(string input) {
            return input.All(c => char.IsDigit(c) || char.IsControl(c));
        }

        private void textBox_ValueChanged(object sender, EventArgs e) {
            if (!int.TryParse(BeginBox.Text, out int begin)) {
                begin = 0;
            }

            if (!int.TryParse(EndBox.Text, out int end)) {
                end = samplesCount;
            }

            InputBeginEnd(begin, end);
        }

        private void InputBeginEnd(int begin, int end) {
            if (samplesCount <= 0 || end - begin <= 0) {
                return;
            }

            end = Math.Clamp(end, 0, samplesCount - 1);
            begin = Math.Clamp(begin, 0, end - 1);

            for (int i = 0; i < charts.Count; i++) {
                foreach (var chart in charts[i]) {
                    chart.Begin = begin;
                    chart.End = end;
                }
            }

            if (GetBegin() != begin) FSelector.LeftSlider = begin;
            if (GetEnd() != end) FSelector.RightSlider = end;
            if (BeginBox.Text != begin.ToString()) BeginBox.Text = begin.ToString();
            if (EndBox.Text != end.ToString()) EndBox.Text = end.ToString();

            if (charts.Count != 0 && charts[0].Count != 0) {
                BeginFrequencyLabel.Content = "Begin Frequency: " + MappingXAxis(charts[0][0].Begin, charts[0][0]);
                EndFrequencyLabel.Content = "End Frequency: " + MappingXAxis(charts[0][0].End, charts[0][0]);
            }
        }

        private void FrequencyChartSetup(ChartLine chart) {
            chart.Margin = new Thickness(0, 2, 0, 2);
            chart.GridDraw = true;
            chart.HAxisTitle = "Частота (Гц)";
            chart.MappingXAxis = MappingXAxis;
            chart.MaxHeightXAxisString = double.MaxValue.ToString();
            chart.ShowCurrentXY = true;
            chart.IsMouseSelect = true;
            chart.OnMouseSelect += OnMouseSelect;
        }

        private void FSelector_IntervalUpdate(object sender, EventArgs e) {
            InputBeginEnd(GetBegin(), GetEnd());
        }
    }
}
