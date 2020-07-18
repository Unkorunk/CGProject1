using System;
using System.Linq;
using System.Windows;
using CGProject1.Chart;
using System.Windows.Controls;
using System.Collections.Generic;
using CGProject1.SignalProcessing;

namespace CGProject1.Pages
{
    public partial class OscillogramsPage : IChannelComponent, IDisposable
    {
        private readonly List<ChartLine> charts = new List<ChartLine>();

        private Signal mySignal;

        private double chartHeight = 100.0;

        private readonly Segment mySegment = new Segment();
        private readonly SegmentControl mySegmentControl;

        public OscillogramsPage()
        {
            InitializeComponent();
            mySegmentControl = new SegmentControl(mySegment);
            ContainerSegmentControl.Child = mySegmentControl;

            mySegmentControl.SetLeftFilter(left =>
            {
                if (mySignal == null) return string.Empty;
                var beginTime = mySignal.GetDateTimeAtIndex(left).ToString("dd-MM-yyyy hh\\:mm\\:ss");
                return $"Begin Time: {beginTime}";

            });
            mySegmentControl.SetRightFilter(right =>
            {
                if (mySignal == null) return string.Empty;
                var endTime = mySignal.GetDateTimeAtIndex(right).ToString("dd-MM-yyyy hh\\:mm\\:ss");
                return $"End Time: {endTime}";
            });

            mySegment.OnChange += MySegment_OnChange;

            var scaleItems = new (string, RoutedEventHandler)[]
            {
                ("Глобальное", GlobalScaling_Click),
                ("Локальное", AutoScaling_Click),
                ("Фиксированное", FixedScaling_Click),
                ("Единое глобальное", UniformGlobalScaling_Click),
                ("Единое локальное", UniformLocalScaling_Click)
            };

            foreach (var (header, handler) in scaleItems)
            {
                var menuItem = new MenuItem {Header = header};
                menuItem.Click += handler;
                ScalingChooser.Items.Add(menuItem);
            }

            if (CountPerPage.Value != null) RecalculateHeight((int) CountPerPage.Value);
        }

        private void SegmentSelector_Segment_OnChange(Segment sender, Segment.SegmentChange segmentChange)
        {
            var isLeft = segmentChange.HasFlag(Segment.SegmentChange.Left);
            var isRight = segmentChange.HasFlag(Segment.SegmentChange.Right);

            if (isLeft && isRight)
            {
                mySegment.SetLeftRight(sender);
            }
            else if (isRight && mySegment.Right != sender.Right)
            {
                mySegment.Right = sender.Right;
            }
            else if (isLeft && mySegment.Left != sender.Left)
            {
                mySegment.Left = sender.Left;
            }
        }

        private void MySegment_OnChange(Segment sender, Segment.SegmentChange segmentChange)
        {
            if (segmentChange.HasFlag(Segment.SegmentChange.Left) ||
                segmentChange.HasFlag(Segment.SegmentChange.Right))
            {
                MainWindow.Instance.UpdateActiveSegment(mySegment.Left, mySegment.Right);

                foreach (var chart in charts)
                {
                    chart.Segment.SetLeftRight(mySegment);
                }
            }
        }

        public void UpdateActiveSegment(int begin, int end)
        {
        }

        public void Reset(Signal newSignal)
        {
            charts.Clear();
            OscillogramsField.Children.Clear();

            mySegmentControl.IsEnabled = newSignal != null;

            if (newSignal != null)
            {
                mySignal = newSignal;

                mySegment.SetMinMax(0, mySignal.SamplesCount - 1);
                mySegment.SetLeftRight(int.MinValue, int.MaxValue);
            }
        }

        public void AddChannel(Channel channel)
        {
            if (channel.SamplesCount != mySignal.SamplesCount) throw new ArgumentException();
            if (charts.Any(item => item.Channel.Name == channel.Name)) return;

            var newChart = new ChartLine(channel)
            {
                IsMouseSelect = true,
                ShowCurrentXY = true,
                GridDraw = true,
                Height = chartHeight,
                Margin = new Thickness(0, 2, 0, 2),

                ContextMenu = new ContextMenu()
            };

            newChart.Segment.SetLeftRight(mySegment);
            newChart.Segment.OnChange += SegmentSelector_Segment_OnChange;

            OscillogramsField.Children.Add(newChart);
            charts.Add(newChart);

            var closeChannel = new MenuItem {Header = "Закрыть канал"};
            closeChannel.Click += (sender, args) =>
            {
                OscillogramsField.Children.Remove(newChart);
                charts.Remove(newChart);
            };

            newChart.ContextMenu.Items.Add(closeChannel);

            #region Scale

            var scaleMenu = new MenuItem {Header = "Масштабирование"};
            newChart.ContextMenu.Items.Add(scaleMenu);

            var scaleItems = new (string, RoutedEventHandler)[]
            {
                (
                    "Глобальное", (sender, args) => newChart.Scaling = ChartLine.ScalingMode.Global
                ),
                (
                    "Локальное", (sender, args) => newChart.Scaling = ChartLine.ScalingMode.Local
                ),
                (
                    "Фиксированное", (sender, args) =>
                    {
                        var settings = new SettingsFixedScale();
                        settings.ShowDialog();
                        if (settings.Status)
                        {
                            newChart.Scaling = ChartLine.ScalingMode.Fixed;
                            newChart.MinFixedScale = settings.From;
                            newChart.MaxFixedScale = settings.To;
                        }
                    }
                ),
                (
                    "Единое глобальное", (sender, args) =>
                    {
                        newChart.GroupedCharts = charts.ToList();
                        newChart.Scaling = ChartLine.ScalingMode.UniformGlobal;
                    }
                ),
                (
                    "Единое локальное", (sender, args) =>
                    {
                        newChart.GroupedCharts = charts.ToList();
                        newChart.Scaling = ChartLine.ScalingMode.UniformLocal;
                    }
                )
            };

            foreach (var (header, handler) in scaleItems)
            {
                var menuItem = new MenuItem {Header = header};
                menuItem.Click += handler;
                scaleMenu.Items.Add(menuItem);
            }

            #endregion

            var statisticsMenuItem = new MenuItem {Header = "Статистика"};
            statisticsMenuItem.Click += (sender, e) => MainWindow.Instance.AddStatistics(channel);
            newChart.ContextMenu.Items.Add(statisticsMenuItem);
        }

        private void ResetSegmentClick(object sender, RoutedEventArgs e)
        {
            mySegment.SetLeftRight(int.MinValue, int.MaxValue);
        }

        private void GlobalScaling_Click(object sender, RoutedEventArgs e)
        {
            foreach (var chart in charts)
            {
                chart.Scaling = ChartLine.ScalingMode.Global;
            }
        }

        private void AutoScaling_Click(object sender, RoutedEventArgs e)
        {
            foreach (var chart in charts)
            {
                chart.Scaling = ChartLine.ScalingMode.Local;
            }
        }

        private void FixedScaling_Click(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsFixedScale();
            settings.ShowDialog();

            if (settings.Status)
            {
                foreach (var chart in charts)
                {
                    chart.Scaling = ChartLine.ScalingMode.Fixed;
                    chart.MinFixedScale = settings.From;
                    chart.MaxFixedScale = settings.To;
                }
            }
        }

        private void UniformLocalScaling_Click(object sender, RoutedEventArgs e)
        {
            foreach (var chart in charts)
            {
                chart.GroupedCharts = charts.ToList();
                chart.Scaling = ChartLine.ScalingMode.UniformLocal;
            }
        }

        private void UniformGlobalScaling_Click(object sender, RoutedEventArgs e)
        {
            foreach (var chart in charts)
            {
                chart.GroupedCharts = charts.ToList();
                chart.Scaling = ChartLine.ScalingMode.UniformGlobal;
            }
        }

        private void RecalculateHeight(int count)
        {
            if (count <= 0) throw new ArgumentException();

            if (OscillogramScrollViewer.ActualHeight <= 0)
            {
                return;
            }

            chartHeight = OscillogramScrollViewer.ActualHeight / count;
            foreach (var chart in charts)
            {
                chart.Height = chartHeight;
            }
        }

        private void CountPerPage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            RecalculateHeight((int) e.NewValue);
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (CountPerPage.Value != null) RecalculateHeight((int) CountPerPage.Value);
        }

        public void Dispose()
        {
            mySegment.OnChange -= MySegment_OnChange;
        }
    }
}
