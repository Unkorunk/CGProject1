using System;
using System.Windows;
using CGProject1.Chart;
using System.Windows.Controls;
using System.Collections.Generic;

namespace CGProject1
{
    /// <summary>
    /// Interaction logic for StatisticsWindow.xaml
    /// </summary>
    public partial class StatisticsWindow : Window
    {
        private readonly Dictionary<string, StatisticsItem> subscribed = new Dictionary<string, StatisticsItem>();

        public StatisticsWindow()
        {
            InitializeComponent();
        }

        public void ReplaceChart(ChartLine chart) {
            if (subscribed.ContainsKey(chart.Channel.Name)) {
                var item = subscribed[chart.Channel.Name];

                item.Subscriber = chart;

                ChartLine.OnChangeIntervalDel onChangeInterval = (sender) =>
                    item.UpdateInfo();

                MenuItem closeChannel = (MenuItem)item.ContextMenu.Items.GetItemAt(0);

                chart.OnChangeInterval += onChangeInterval;
                closeChannel.Click += (object sender, RoutedEventArgs args) =>
                    chart.OnChangeInterval -= onChangeInterval;
                this.Closed += (object sender, EventArgs e) =>
                    chart.OnChangeInterval -= onChangeInterval;
            }
        }

        public void Update(ChartLine chart, bool fromMainWindow)
        {
            if (!subscribed.ContainsKey(chart.Channel.Name))
            {
                StatisticsItem item = new StatisticsItem(chart);

                ChartLine.OnChangeIntervalDel onChangeInterval = (sender) =>
                    item.UpdateInfo();

                MenuItem closeChannel = new MenuItem();
                closeChannel.Header = "Закрыть канал";

                if (!fromMainWindow)
                {
                    chart.OnChangeInterval += onChangeInterval;
                    closeChannel.Click += (object sender, RoutedEventArgs args) =>
                        chart.OnChangeInterval -= onChangeInterval;
                    this.Closed += (object sender, EventArgs e) =>
                        chart.OnChangeInterval -= onChangeInterval;
                }

                closeChannel.Click += (object sender, RoutedEventArgs args) =>
                {
                    subscribed.Remove(chart.Channel.Name);
                    ChannelsPanel.Children.Remove(item);
                };
                this.Closed += (object sender, EventArgs e) =>
                {
                    subscribed.Remove(chart.Channel.Name);
                    ChannelsPanel.Children.Remove(item);
                };

                item.ContextMenu = new ContextMenu();
                item.ContextMenu.Items.Add(closeChannel);

                ChannelsPanel.Children.Add(item);
                subscribed.Add(chart.Channel.Name, item);
            }
        }
    }
}
