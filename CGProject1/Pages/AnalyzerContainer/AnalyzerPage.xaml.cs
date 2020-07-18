using System;
using System.Linq;
using System.Windows;
using CGProject1.Chart;
using System.Windows.Input;
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
            }

            mySegmentControl = new SegmentControl(myVisibleSegment);
            ContainerSegmentControl.Child = mySegmentControl;
            
            myActiveSegment.OnChange += MyActiveSegment_OnChange;
            myVisibleSegment.OnChange += MyVisibleSegment_OnChange;

            // TODO calculate frequency
            mySegmentControl.SetLeftFilter(left => mySignal == null ? string.Empty : "LEFT FREQUENCY: TODO");
            mySegmentControl.SetRightFilter(right => mySignal == null ? string.Empty : "RIGHT FREQUENCY: TODO");

            if (CountPerPage.Value != null) RecalculateHeight((int) CountPerPage.Value);
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
            // Clear
            foreach (var group in groups)
            {
                group.Factories.Clear();
            }

            ClearSpectrePanel();

            // Init
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
                group.Factories.Add(factory);
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
                    
                    myVisibleSegment.SetMinMax(0, factory.Analyzer.SamplesCount - 1);
                }
            }
        }

        private void UpdatePanel()
        {
            if (SpectrePanel == null || ComboBoxMode == null) return;
            
            ClearSpectrePanel();
            foreach (var chartLine in groups[ComboBoxMode.SelectedIndex].Process())
            {
                // TODO disable horizontal axis for charts between first and last
                chartLine.Height = chartHeight;
                chartLine.Segment.SetSegment(myVisibleSegment);
                chartLine.Segment.OnChange += ChartLine_Segment_OnChange;
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
        }
    }
}