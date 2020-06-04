using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CGProject1.SignalProcessing;

namespace CGProject1 {
    public partial class AnalyzerWindow : Window {

        private List<List<Chart>> charts = new List<List<Chart>>();
        private int begin;
        private int end;

        public AnalyzerWindow(int begin, int end) {
            InitializeComponent();

            for (int i = 0; i < ComboBoxMode.Items.Count; i++) charts.Add(new List<Chart>());

            BeginSelector.Text = begin.ToString();
            EndSelector.Text = end.ToString();

            this.begin = begin;
            this.end = end;
        }

        public void AddChannel(Channel channel) {
            
            Analyzer.SetupChannel(channel, begin, end);

            Channel amp = Analyzer.AmplitudeSpectre(0);

            var newDx = 1.0 / (2 * channel.DeltaTime * amp.SamplesCount);
            amp.SamplingFrq = 1.0 / newDx;

            var ampChart = new Chart(amp);
            ampChart.Height = 200;
            ampChart.Begin = 0;
            ampChart.End = amp.SamplesCount;
            ampChart.Margin = new Thickness(0, 2, 0, 2);
            ampChart.GridDraw = true;
            ampChart.HAxisTitle = "Частота (Гц)";
            charts[1].Add(ampChart);

            var psd = Analyzer.PowerSpectralDensity(0);
            psd.SamplingFrq = 1.0 / newDx;
            var psdChart = new Chart(psd);
            psdChart.Height = 200;
            psdChart.Begin = 0;
            psdChart.End = psd.SamplesCount;
            psdChart.Margin = new Thickness(0, 2, 0, 2);
            psdChart.GridDraw = true;
            psdChart.HAxisTitle = "Частота (Гц)";
            charts[0].Add(psdChart);

            var lg = Analyzer.LogarithmicSpectre(0);
            lg.SamplingFrq = 1.0 / newDx;
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

        void UpdatePanel()
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

        private void ComboBoxMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdatePanel();
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
