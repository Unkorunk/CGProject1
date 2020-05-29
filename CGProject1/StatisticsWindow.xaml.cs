﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CGProject1
{
    /// <summary>
    /// Interaction logic for StatisticsWindow.xaml
    /// </summary>
    public partial class StatisticsWindow : Window
    {
        private List<string> subscribed = new List<string>();

        public StatisticsWindow()
        {
            InitializeComponent();
        }

        public void Update(Chart chart)
        {
            if (!subscribed.Exists(chartName => chartName == chart.Channel.Name))
            {
                subscribed.Add(chart.Channel.Name);

                Border border = new Border();
                border.BorderThickness = new Thickness(1.0);
                border.BorderBrush = Brushes.Black;

                Grid statisticGrid = new Grid();
                border.Child = statisticGrid;

                StackPanel panel = new StackPanel();
                statisticGrid.Children.Add(panel);

                Label channelNameLabel = new Label();
                panel.Children.Add(channelNameLabel);
                Label intervalLabel = new Label();
                panel.Children.Add(intervalLabel);

                Label statLabel = new Label();
                panel.Children.Add(statLabel);

                Action<Chart> updateInfo = (sender) =>
                {
                    channelNameLabel.Content = "Name: " + sender.Channel.Name;
                    intervalLabel.Content = "Begin: " + (sender.Begin + 1) + "; End: " + (sender.End + 1);

                    statLabel.Content = "";

                    double avg = CalcAvg(sender);
                    statLabel.Content += "Среднее: " + Math.Round(avg, 2) + Environment.NewLine;

                    double disp = CalcDisp(sender);
                    statLabel.Content += "Дисперсия: " + Math.Round(disp, 2) + Environment.NewLine;
                    statLabel.Content += "Среднеквадратичное отклонение: " + Math.Round(Math.Sqrt(disp), 2) + Environment.NewLine;

                    statLabel.Content += "Коэффициент вариации: " + Math.Round(Math.Sqrt(disp) / avg, 2) + Environment.NewLine;
                    statLabel.Content += "Коэффициент асимметрии: " + Math.Round(CalcCoefAsim(sender), 2) + Environment.NewLine;
                    statLabel.Content += "Коэффициент эксцесса: " + Math.Round(CalcCoefExces(sender), 2) + Environment.NewLine;

                    statLabel.Content += "Минимальное значение сигнала: " + Math.Round(sender.Channel.values
                        .Where((_, idx) => idx >= sender.Begin && idx <= sender.End)
                        .Min(), 2) + Environment.NewLine;
                    statLabel.Content += "Максимальное значение сигнала: " + Math.Round(sender.Channel.values
                        .Where((_, idx) => idx >= sender.Begin && idx <= sender.End)
                        .Max(), 2) + Environment.NewLine;

                    statLabel.Content += "Квантиль порядка 0.05: " + Math.Round(CalcKvantil(sender, 0.05), 2) + Environment.NewLine;
                    statLabel.Content += "Квантиль порядка 0.95: " + Math.Round(CalcKvantil(sender, 0.95), 2) + Environment.NewLine;
                    statLabel.Content += "Медиана: " + Math.Round(CalcKvantil(sender, 0.5), 2) + Environment.NewLine;
                };

                updateInfo(chart);

                Chart.OnChangeIntervalDel onChangeInterval = (sender) => updateInfo(sender);
                chart.OnChangeInterval += onChangeInterval;

                MenuItem closeChannel = new MenuItem();
                closeChannel.Header = "Закрыть канал";
                closeChannel.Click += (object sender, RoutedEventArgs args) =>
                {
                    chart.OnChangeInterval -= onChangeInterval;
                    subscribed.Remove(chart.Channel.Name);
                    ChannelsPanel.Children.Remove(border);
                };

                statisticGrid.ContextMenu = new ContextMenu();
                statisticGrid.ContextMenu.Items.Add(closeChannel);

                ChannelsPanel.Children.Add(border);
            }
        }

        public double CalcAvg(Chart chart)
        {
            if (chart.Length <= 0) return 0.0;

            return chart.Channel.values
                .Where((_, idx) => idx >= chart.Begin && idx <= chart.End)
                .Average();
        }

        public double CalcDisp(Chart chart)
        {
            if (chart.Length <= 0) return 0.0;

            double avg = CalcAvg(chart);
            double disp = 0.0;
            for (int i = chart.Begin; i <= chart.End; i++)
            {
                disp += Math.Pow(chart.Channel.values[i] - avg, 2);
            }
            return disp /= chart.Length;
        }

        public double CalcCoefAsim(Chart chart)
        {
            if (chart.Length <= 0) return 0.0;

            double mse3 = Math.Pow(CalcDisp(chart), 3.0 / 2.0);
            double avg = CalcAvg(chart);
            double coef = 0.0;
            for (int i = chart.Begin; i <= chart.End; i++)
            {
                coef += Math.Pow(chart.Channel.values[i] - avg, 3);
            }
            coef /= chart.Length * mse3;
            return coef;
        }

        public double CalcCoefExces(Chart chart)
        {
            if (chart.Length <= 0) return 0.0;

            double mse4 = Math.Pow(CalcDisp(chart), 2);
            double avg = CalcAvg(chart);
            double coef = 0.0;
            for (int i = chart.Begin; i <= chart.End; i++)
            {
                coef += Math.Pow(chart.Channel.values[i] - avg, 4);
            }
            coef /= chart.Length * mse4;
            return coef - 3;
        }

        public double CalcKvantil(Chart chart, double p)
        {
            if (chart.Length <= 0) return 0.0;

            p = Math.Clamp(p, 0.0, 1.0);
            return chart.Channel.values
                .Where((_, idx) => idx >= chart.Begin && idx <= chart.End)
                .OrderBy(x => x).ToList()[(int)(p * (chart.Length - 1))];
        }
    }
}
