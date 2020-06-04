using System.Windows;

using CGProject1.SignalProcessing;

namespace CGProject1 {
    public partial class AnalyzerWindow : Window {
        public AnalyzerWindow() {
            InitializeComponent();
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
                SpectrePanel.Children.Add(ampChart);

                var psd = Analyzer.PowerSpectralDensity(0);
                psd.SamplingFrq = 1.0 / newDx;
                var psdChart = new Chart(psd);
                psdChart.Height = 200;
                psdChart.Begin = 0;
                psdChart.End = amp.SamplesCount;
                psdChart.Margin = new Thickness(0, 2, 0, 2);
                psdChart.GridDraw = true;
                SpectrePanel.Children.Add(psdChart);
            }
            

            //{
            //    Analyzer.SetupChannelSlow(channel, 0, channel.SamplesCount - 1);

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
    }
}
