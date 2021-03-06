﻿using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using CGProject1.Chart;
using CGProject1.Pages;
using CGProject1.SignalProcessing;

namespace CGProject1 {
    public partial class ChannelsPage : Page, IChannelComponent {
        private readonly List<ChartLine> charts;

        public ChannelsPage() {
            InitializeComponent();
            charts = new List<ChartLine>();
        }

        public void Reset(Signal signal) {
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
                MainWindow.Instance.AddOscillogram(channel);
            };
            chart.ContextMenu.Items.Add(item1);

            var item2 = new MenuItem();
            item2.Header = "Статистика";
            item2.Click += (object sender, RoutedEventArgs args) => {
                MainWindow.Instance.AddStatistics(channel);
            };
            chart.ContextMenu.Items.Add(item2);

            var item3 = new MenuItem();
            item3.Header = "Анализ Фурье";
            item3.Click += (object sender, RoutedEventArgs args) => {
                MainWindow.Instance.AddAnalyze(channel);
            };
            chart.ContextMenu.Items.Add(item3);

            var item4 = new MenuItem();
            item4.Header = "Спектрограмма";
            item4.Click += (object sender, RoutedEventArgs args) => {
                MainWindow.Instance.AddSpectrogram(channel);
            };
            chart.ContextMenu.Items.Add(item4);

            ChannelsPanel.Children.Add(chart);
        }

        public void UpdateActiveSegment(int begin, int end) { }
    }
}
