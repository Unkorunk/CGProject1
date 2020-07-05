using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CGProject1.Pages;

namespace CGProject1 {
    /// <summary>
    /// Interaction logic for StatisticsPage.xaml
    /// </summary>
    public partial class StatisticsPage : Page, IChannelComponent {
        private readonly Dictionary<string, StatisticsItem> subscribed = new Dictionary<string, StatisticsItem>();

        private int begin;
        private int end;

        public StatisticsPage() {
            InitializeComponent();
        }

        public void Reset(Signal signal) {
            subscribed.Clear();
            ChannelsPanel.Children.Clear();
        }

        public void AddChannel(Channel chart) {
            if (!subscribed.ContainsKey(chart.Name)) {
                StatisticsItem item = new StatisticsItem(chart);

                //ChartLine.OnChangeIntervalDel onChangeInterval = (sender) =>
                //    item.UpdateInfo(begin, end);

                MenuItem closeChannel = new MenuItem();
                closeChannel.Header = "Закрыть канал";

                closeChannel.Click += (object sender, RoutedEventArgs args) => {
                    subscribed.Remove(chart.Name);
                    ChannelsPanel.Children.Remove(item);
                };
                //this.Closed += (object sender, EventArgs e) => {
                //    subscribed.Remove(chart.Channel.Name);
                //    ChannelsPanel.Children.Remove(item);
                //};

                item.ContextMenu = new ContextMenu();
                item.ContextMenu.Items.Add(closeChannel);

                ChannelsPanel.Children.Add(item);
                subscribed.Add(chart.Name, item);
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
