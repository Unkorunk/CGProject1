using System;
using System.Windows;
using CGProject1.Chart;
using System.Globalization;
using System.Windows.Controls;
using CGProject1.SignalProcessing;
using System.Diagnostics.CodeAnalysis;

namespace CGProject1.Pages.AnalyzerContainer
{
    public class ChartLineFactory
    {
        public Analyzer Analyzer { get; }

        private bool deleted = false;

        public bool Deleted
        {
            get => deleted;
            private set
            {
                deleted = value;
                if (deleted) OnDeleted?.Invoke();
            }
        }

        public delegate void DelDeleted();

        public event DelDeleted OnDeleted;

        public ChartLineFactory([NotNull] Analyzer analyzer)
        {
            Analyzer = analyzer;
        }
        
        // Power spectral density
        public ChartLine MakePsd(bool logarithmic)
        {
            if (Deleted) throw new InvalidOperationException();
            
            var channel = logarithmic ? Analyzer.LogarithmicPsd() : Analyzer.PowerSpectralDensity();
            return Setup(channel);
        }

        // Amplitude spectral density
        public ChartLine MakeAsd(bool logarithmic)
        {
            if (Deleted) throw new InvalidOperationException();
            
            var channel = logarithmic ? Analyzer.LogarithmicAsd() : Analyzer.AmplitudeSpectralDensity();
            return Setup(channel);
        }

        private ChartLine Setup(Channel channel)
        {
            if (Deleted) throw new InvalidOperationException();

            var chartLine = new ChartLine(channel)
            {
                Margin = new Thickness(0, 2, 0, 2),
                GridDraw = true,
                HAxisTitle = "Frequency (Hz)",
                MappingXAxis = MappingXAxis,
                MaxHeightXAxisString = double.MaxValue.ToString(CultureInfo.InvariantCulture),
                ShowCurrentXY = true,
                IsMouseSelect = true,
                Scaling = ChartLine.ScalingMode.LocalZeroed,
                ContextMenu = new ContextMenu()
            };
            
            var menuItem = new MenuItem {Header = "Close"};
            menuItem.Click += (sender, args) => Deleted = true;
            chartLine.ContextMenu.Items.Add(menuItem);

            return chartLine;
        }
        
        public static string MappingXAxis(int idx, [NotNull] ChartLine chart)
        {
            var curVal = chart.Channel.DeltaTime * idx;
            return curVal.ToString("N6", CultureInfo.InvariantCulture);
        }
    }
}