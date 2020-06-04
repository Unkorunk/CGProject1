using System.Collections.Generic;
using System.Windows;
using CGProject1.SignalProcessing;

namespace CGProject1 {
    public partial class AnalyzerWindow : Window {

        private List<List<Chart>> charts = new List<List<Chart>>();

        public AnalyzerWindow() {
            InitializeComponent();

            for (int i = 0; i < ComboBoxMode.Items.Count; i++) charts.Add(new List<Chart>());
        }

        public void AddChannel(Channel channel) {
            {
                Analyzer.SetupChannel(channel, 0, channel.SamplesCount - 1);

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
                psdChart.End = amp.SamplesCount;
                psdChart.Margin = new Thickness(0, 2, 0, 2);
                psdChart.GridDraw = true;
                psdChart.HAxisTitle = "Частота (Гц)";
                charts[0].Add(psdChart);

                UpdatePanel();
            }


            //{
            //    Analyzer.SetupSlowChannel(channel, 0, channel.SamplesCount - 1);

            //    Channel amp = Analyzer.AmplitudeSpectre(0);

            //    var newDx = 1.0 / (2 * channel.DeltaTime * amp.SamplesCount);
            //    amp.SamplingFrq = 1.0 / newDx;

            //    var ampChart = new Chart(amp);
            //    ampChart.Height = 200;
            //    ampChart.Begin = 0;
            //    ampChart.End = amp.SamplesCount;
            //    ampChart.Margin = new Thickness(0, 2, 0, 2);
            //    ampChart.GridDraw = true;
            //    SpectrePanel.Children.Add(ampChart);

            //    var psd = Analyzer.PowerSpectralDensity(0);
            //    psd.SamplingFrq = 1.0 / newDx;
            //    var psdChart = new Chart(psd);
            //    psdChart.Height = 200;
            //    psdChart.Begin = 0;
            //    psdChart.End = amp.SamplesCount;
            //    psdChart.Margin = new Thickness(0, 2, 0, 2);
            //    psdChart.GridDraw = true;
            //    SpectrePanel.Children.Add(psdChart);
            //}
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
    }
}
