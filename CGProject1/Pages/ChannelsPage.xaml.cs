using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using CGProject1.Chart;

namespace CGProject1 {
    public partial class ChannelsPage : Page {
        private readonly List<ChartLine> charts;

        public ChannelsPage() {
            InitializeComponent();
            charts = new List<ChartLine>();
        }

        public void Reset() {
            charts.Clear();
            ChannelsPanel.Children.Clear();
        }

        public void AddChannel(Channel channel) {
            var chart = new ChartLine(channel);
            chart.Height = 100;

            charts.Add(chart);

            chart.ContextMenu = new ContextMenu();

            var item1 = new MenuItem();
            item1.Header = "Осциллограмма";
            item1.Click += (object sender, RoutedEventArgs args) => {
                MainWindow.instance.AddOscillogram(chart);
            };
            chart.ContextMenu.Items.Add(item1);

            var item2 = new MenuItem();
            item2.Header = "Статистика";
            item2.Click += (object sender, RoutedEventArgs args) => {
                MainWindow.instance.AddStatistics(chart);
            };
            chart.ContextMenu.Items.Add(item2);

            var item3 = new MenuItem();
            item3.Header = "Анализ";
            item3.Click += (object sender, RoutedEventArgs args) => {
                MainWindow.instance.AddAnalyze(chart);
            };
            chart.ContextMenu.Items.Add(item3);

            var item4 = new MenuItem();
            item4.Header = "Спектрограмма";
            item4.Click += (object sender, RoutedEventArgs args) => {
                MainWindow.instance.AddSpectrogram(chart);
            };
            chart.ContextMenu.Items.Add(item4);

            chart.Begin = 0;
            chart.End = channel.SamplesCount;

            ChannelsPanel.Children.Add(chart);
        }
    }
}
