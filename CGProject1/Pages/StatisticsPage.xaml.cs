using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CGProject1.Chart;

namespace CGProject1 {
    /// <summary>
    /// Interaction logic for StatisticsPage.xaml
    /// </summary>
    public partial class StatisticsPage : Page {
        private readonly Dictionary<string, StatisticsItem> subscribed = new Dictionary<string, StatisticsItem>();

        private int begin;
        private int end;

        public StatisticsPage() {
            InitializeComponent();
        }

        public void Reset() {
            subscribed.Clear();
            ChannelsPanel.Children.Clear();
        }

        public void Update(ChartLine chart) {
            if (!subscribed.ContainsKey(chart.Channel.Name)) {
                StatisticsItem item = new StatisticsItem(chart);

                ChartLine.OnChangeIntervalDel onChangeInterval = (sender) =>
                    item.UpdateInfo(begin, end);

                MenuItem closeChannel = new MenuItem();
                closeChannel.Header = "Закрыть канал";

                closeChannel.Click += (object sender, RoutedEventArgs args) => {
                    subscribed.Remove(chart.Channel.Name);
                    ChannelsPanel.Children.Remove(item);
                };
                //this.Closed += (object sender, EventArgs e) => {
                //    subscribed.Remove(chart.Channel.Name);
                //    ChannelsPanel.Children.Remove(item);
                //};

                item.ContextMenu = new ContextMenu();
                item.ContextMenu.Items.Add(closeChannel);

                ChannelsPanel.Children.Add(item);
                subscribed.Add(chart.Channel.Name, item);
            }
        }

        public void UpdateActiveSegment(int begin, int end) {
            this.begin = begin;
            this.end = end;

            foreach (var item in subscribed.Values) {
                item.UpdateInfo(begin, end);
            }
        }
    }
}
