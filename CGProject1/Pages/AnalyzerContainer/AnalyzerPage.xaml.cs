using System;
using System.Linq;
using System.Windows;
using CGProject1.Chart;
using System.Windows.Input;
using System.Globalization;
using System.Windows.Controls;
using System.Collections.Generic;
using CGProject1.SignalProcessing;

namespace CGProject1.Pages.AnalyzerContainer
{
    public partial class AnalyzerPage : IChannelComponent, IDisposable
    {
        private readonly Segment myActiveSegment = new Segment();
        private readonly Segment myVisibleSegment = new Segment();
        private readonly SegmentControl mySegmentControl;

        private double chartHeight = 100.0;

        private bool initialized, isFirstInit;

        private readonly IReadOnlyList<GroupChartLineFactory> groups = new[]
        {
            new GroupChartLineFactory("PSD", factory => factory.MakePsd(false)),
            new GroupChartLineFactory("ASD", factory => factory.MakeAsd(false)),
            new GroupChartLineFactory("Logarithmic PSD", factory => factory.MakePsd(true)),
            new GroupChartLineFactory("Logarithmic ASD", factory => factory.MakeAsd(true))
        };

        private Signal mySignal;

        public AnalyzerPage()
        {
            InitializeComponent();

            foreach (var group in groups)
            {
                ComboBoxMode.Items.Add(group.Title);
                group.OnUpdate += Group_OnUpdate;
            }

            mySegmentControl = new SegmentControl(myVisibleSegment);
            ContainerSegmentControl.Child = mySegmentControl;

            myActiveSegment.OnChange += MyActiveSegment_OnChange;
            myVisibleSegment.OnChange += MyVisibleSegment_OnChange;
            
            mySegmentControl.SetLeftFilter(left => mySignal == null ? string.Empty : $"Left Frequency: {GetFrequency(true)}");
            mySegmentControl.SetRightFilter(right => mySignal == null ? string.Empty : $"Right Frequency: {GetFrequency(false)}");

            if (CountPerPage.Value != null) RecalculateHeight((int) CountPerPage.Value);
        }

        private void Group_OnUpdate()
        {
            UpdatePanel();
        }
        
        private void MyVisibleSegment_OnChange(Segment sender, Segment.SegmentChange segmentChange)
        {
            if (segmentChange != Segment.SegmentChange.None)
            {
                UpdatePanel();
            }
        }

        private void ChartLine_Segment_OnChange(Segment sender, Segment.SegmentChange segmentChange)
        {
            var isLeft = segmentChange.HasFlag(Segment.SegmentChange.Left);
            var isRight = segmentChange.HasFlag(Segment.SegmentChange.Right);

            if (isLeft && isRight)
            {
                myVisibleSegment.SetLeftRight(sender);
            }
            else if (isRight && myVisibleSegment.Right != sender.Right)
            {
                myVisibleSegment.Right = sender.Right;
            }
            else if (isLeft && myVisibleSegment.Left != sender.Left)
            {
                myVisibleSegment.Left = sender.Left;
            }
        }

        private void MyActiveSegment_OnChange(Segment sender, Segment.SegmentChange segmentChange)
        {
            if (segmentChange != Segment.SegmentChange.None)
            {
                LeftTextBox.Text = sender.Left.ToString();
                RightTextBox.Text = sender.Right.ToString();

                initialized = false;

                UpdateAnalyzers();
                UpdatePanel();
            }
        }

        private ZeroModeEnum GetZeroMode()
        {
            return ZeroModeSelector.SelectedIndex switch
            {
                0 => ZeroModeEnum.Nothing,
                1 => ZeroModeEnum.Null,
                2 => ZeroModeEnum.Smooth,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public void UpdateActiveSegment(int left, int right)
        {
            myActiveSegment.SetLeftRight(left, right);
        }

        public void Reset(Signal newSignal)
        {
            foreach (var group in groups)
            {
                group.Clear();
            }

            ClearSpectrePanel();
            initialized = false;
            isFirstInit = true;

            mySegmentControl.IsEnabled = newSignal != null;

            if (newSignal != null)
            {
                mySignal = newSignal;

                myActiveSegment.SetMinMax(0, mySignal.SamplesCount - 1);
                myActiveSegment.SetLeftRight(int.MinValue, int.MaxValue);
            }
        }

        public void AddChannel(Channel channel)
        {
            var analyzer = new Analyzer(channel);

            var factory = new ChartLineFactory(analyzer);
            foreach (var group in groups)
            {
                group.Add(factory);
            }

            UpdateAnalyzers();
            UpdatePanel();
        }

        private void ResetSegmentClick(object sender, RoutedEventArgs e)
        {
            myVisibleSegment.SetLeftRight(int.MinValue, int.MaxValue);
        }

        private void ComboBoxMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePanel();
        }

        private void SelectInterval(object sender, RoutedEventArgs e)
        {
            UpdateAnalyzers();
            UpdatePanel();
        }

        #region TextBox Functions

        private bool TextIsNumeric(string input)
        {
            return input.All(c => char.IsDigit(c) || char.IsControl(c));
        }

        private void PreviewTextInputHandle(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !TextIsNumeric(e.Text);
        }

        private void PreviewPastingHandle(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var input = (string) e.DataObject.GetData(typeof(string));
                if (!TextIsNumeric(input)) e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        #endregion TextBox Functions

        #region Page Functions

        private void RecalculateHeight(int count)
        {
            if (AnalyzerScrollViewer == null || SpectrePanel == null) return;

            if (AnalyzerScrollViewer.ActualHeight <= 0) return;

            chartHeight = AnalyzerScrollViewer.ActualHeight / count;

            foreach (var child in SpectrePanel.Children)
            {
                if (child != null && child is ChartLine chartLine)
                {
                    chartLine.Height = chartHeight;
                }
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

        #endregion Page Functions

        private void ZeroModeSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAnalyzers();
            UpdatePanel();
        }

        private void UpdateAnalyzers()
        {
            foreach (var group in groups)
            {
                foreach (var factory in group.Factories)
                {
                    factory.Analyzer.ZeroMode = GetZeroMode();
                    factory.Analyzer.SetupChannel(myActiveSegment.Left, myActiveSegment.Right);
                    if (int.TryParse(HalfWindowTextBox.Text, out var halfWindow))
                    {
                        factory.Analyzer.HalfWindowSmoothing = halfWindow;
                    }
                }
            }

            AfterUpdateAnalyzers();
        }

        private void UpdatePanel()
        {
            if (SpectrePanel == null || ComboBoxMode == null) return;

            var count = groups[ComboBoxMode.SelectedIndex].Factories.Count;
            var current = 0;

            ClearSpectrePanel();
            
            foreach (var chartLine in groups[ComboBoxMode.SelectedIndex].Process())
            {
                chartLine.Height = chartHeight;
                chartLine.Segment.SetSegment(myVisibleSegment);
                chartLine.Segment.OnChange += ChartLine_Segment_OnChange;

                chartLine.DisplayHAxisInfo = false;
                chartLine.DisplayHAxisTitle = false;

                if (current == 0 || current == count - 1)
                {
                    chartLine.DisplayHAxisInfo = true;
                    chartLine.DisplayHAxisTitle = true;
                    chartLine.HAxisAlligment = current == 0 ? ChartLine.HAxisAlligmentEnum.Top : ChartLine.HAxisAlligmentEnum.Bottom;
                }

                current++;

                SpectrePanel.Children.Add(chartLine);
            }
        }

        private void ClearSpectrePanel()
        {
            if (SpectrePanel == null) return;

            foreach (var child in SpectrePanel.Children)
            {
                if (child != null && child is ChartLine chartLine)
                {
                    chartLine.Segment.OnChange -= ChartLine_Segment_OnChange;
                }
            }

            SpectrePanel.Children.Clear();
        }

        public void Dispose()
        {
            myActiveSegment.OnChange -= MyActiveSegment_OnChange;
            myVisibleSegment.OnChange -= MyVisibleSegment_OnChange;
            foreach (var group in groups)
            {
                group.OnUpdate -= Group_OnUpdate;
            }
        }

        private ChartLine GetFirstChartLineForCurrentMode()
        {
            if (SpectrePanel == null) return null;

            foreach (var child in SpectrePanel.Children)
            {
                if (child != null && child is ChartLine chartLine)
                {
                    return chartLine;
                }
            }

            return null;
        }

        private string GetFrequency(bool left)
        {
            var chartLine = GetFirstChartLineForCurrentMode();
            if (chartLine == null) return double.NaN.ToString(CultureInfo.InvariantCulture);

            return left
                ? ChartLineFactory.MappingXAxis(chartLine.Segment.Left, chartLine)
                : ChartLineFactory.MappingXAxis(chartLine.Segment.Right, chartLine);
        }

        private void AfterUpdateAnalyzers()
        {
            if (ComboBoxMode != null && ComboBoxMode.SelectedItem != null && !initialized)
            {
                var group = groups[ComboBoxMode.SelectedIndex].Factories.FirstOrDefault();

                if (group != null)
                {
                    var oldSegment = new Segment();
                    oldSegment.SetSegment(myVisibleSegment);

                    myVisibleSegment.SetMinMax(0, Math.Max(0, group.Analyzer.SamplesCount - 1));

                    if (isFirstInit)
                    {
                        myVisibleSegment.SetLeftRight(int.MinValue, int.MaxValue);
                        isFirstInit = false;
                    }
                    else
                    {
                        int newLength = myVisibleSegment.MaxValue - myVisibleSegment.MinValue + 1;
                        int oldLength = oldSegment.MaxValue - oldSegment.MinValue + 1;

                        int left = (int) Math.Round((oldSegment.Left - oldSegment.MinValue + 1.0) * newLength / oldLength +
                            myVisibleSegment.MinValue - 1);
                        int right = (int) Math.Round((oldSegment.Right - oldSegment.MinValue + 1.0) * newLength / oldLength +
                            myVisibleSegment.MinValue - 1);

                        myVisibleSegment.SetLeftRight(left, right);
                    }

                    initialized = true;
                }
            }
        }
    }
}