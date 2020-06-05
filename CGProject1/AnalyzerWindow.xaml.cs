using System;
using System.Collections.Generic;
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

            analyzers.Add(analyzer);

            Channel amp = analyzer.AmplitudeSpectre();
            var ampChart = new Chart(amp);
            ampChart.Height = 200;
            ampChart.Begin = 0;
            ampChart.End = amp.SamplesCount;
            ampChart.Margin = new Thickness(0, 2, 0, 2);
            ampChart.GridDraw = true;
            ampChart.HAxisTitle = "Частота (Гц)";
            charts[1].Add(ampChart);

            Channel psd = analyzer.PowerSpectralDensity();
            var psdChart = new Chart(psd);
            psdChart.Height = 200;
            psdChart.Begin = 0;
            psdChart.End = psd.SamplesCount;
            psdChart.Margin = new Thickness(0, 2, 0, 2);
            psdChart.GridDraw = true;
            psdChart.HAxisTitle = "Частота (Гц)";
            charts[0].Add(psdChart);

            Channel lg = analyzer.LogarithmicSpectre();
            var logChart = new Chart(lg);
            logChart.Height = 200;
            logChart.Begin = 0;
            logChart.End = lg.SamplesCount;
            logChart.Margin = new Thickness(0, 2, 0, 2);
            logChart.GridDraw = true;
            logChart.HAxisTitle = "Частота (Гц)";
            charts[2].Add(logChart);
            charts[3].Add(logChart);

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

                Channel amp = analyzer.AmplitudeSpectre();

                var ampChart = new Chart(amp);
                ampChart.Height = 200;
                ampChart.Begin = 0;
                ampChart.End = amp.SamplesCount;
                ampChart.Margin = new Thickness(0, 2, 0, 2);
                ampChart.GridDraw = true;
                ampChart.HAxisTitle = "Частота (Гц)";
                charts[1].Add(ampChart);

                var psd = analyzer.PowerSpectralDensity();
                var psdChart = new Chart(psd);
                psdChart.Height = 200;
                psdChart.Begin = 0;
                psdChart.End = psd.SamplesCount;
                psdChart.Margin = new Thickness(0, 2, 0, 2);
                psdChart.GridDraw = true;
                psdChart.HAxisTitle = "Частота (Гц)";
                charts[0].Add(psdChart);

                var lg = analyzer.LogarithmicSpectre();
                var logChart = new Chart(lg);
                logChart.Height = 200;
                logChart.Begin = 0;
                logChart.End = lg.SamplesCount;
                logChart.Margin = new Thickness(0, 2, 0, 2);
                logChart.GridDraw = true;
                logChart.HAxisTitle = "Частота (Гц)";
                charts[2].Add(logChart);
                charts[3].Add(logChart);
            }

            UpdatePanel();
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

        private bool TextIsNumeric(string input) {
            return input.All(c => char.IsDigit(c) || char.IsControl(c));
        }
    }
}
