using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CGProject1.Chart;
using CGProject1.SignalProcessing;

namespace CGProject1
{
    public class ChartAnalyzerFactory
    {
        public enum AnalyzerType
        {
            PowerSpectralDensity,
            AmplitudeSpectralDensity
        }
        
        public Analyzer Analyzer { get; }

        public ChartAnalyzerFactory(Analyzer analyzer)
        {
            this.Analyzer = analyzer;
        }

        private string MappingXAxis(int idx, ChartLine chart)
        {
            double curVal = chart.Channel.DeltaTime * idx;
            return curVal.ToString("N6", CultureInfo.InvariantCulture);
        }
        
        private void FrequencyChartSetup(ChartLine chart)
        {
            chart.Margin = new Thickness(0, 2, 0, 2);
            chart.GridDraw = true;
            chart.HAxisTitle = "Частота (Гц)";
            chart.MappingXAxis = MappingXAxis;
            chart.MaxHeightXAxisString = double.MaxValue.ToString(CultureInfo.InvariantCulture);
            chart.ShowCurrentXY = true;
            chart.IsMouseSelect = true;
        }

        public ChartLine Factory(AnalyzerType analyzerType, bool logarithmic)
        {
            var channel = analyzerType switch
            {
                AnalyzerType.PowerSpectralDensity => logarithmic
                    ? Analyzer.LogarithmicPsd()
                    : Analyzer.PowerSpectralDensity(),
                AnalyzerType.AmplitudeSpectralDensity => logarithmic
                    ? Analyzer.LogarithmicAsd()
                    : Analyzer.AmplitudeSpectralDensity(),
                _ => throw new NotSupportedException()
            };

            var chartLine = new ChartLine(channel)
            {
                Scaling = ChartLine.ScalingMode.LocalZeroed,
                ContextMenu = new ContextMenu()
            };

            FrequencyChartSetup(chartLine);

            return chartLine;
        }
        
    }
}