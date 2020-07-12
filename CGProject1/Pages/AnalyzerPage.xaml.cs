using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using CGProject1.SignalProcessing;

namespace CGProject1.Pages
{
    public partial class AnalyzerPage : IChannelComponent, IDisposable
    {
        private readonly Segment mySegment = new Segment();
        private readonly SegmentControl mySegmentControl;
        
        private Signal mySignal;
        
        public AnalyzerPage()
        {
            InitializeComponent();
            mySegmentControl = new SegmentControl(mySegment);
            ContainerSegmentControl.Child = mySegmentControl;

            mySegmentControl.SetLeftFilter(left =>
            {
                if (mySignal == null) return string.Empty;
                var beginTime = mySignal.GetDateTimeAtIndex(left).ToString("dd-MM-yyyy hh\\:mm\\:ss");
                return $"Begin Frequency: {beginTime}";

            });
            mySegmentControl.SetRightFilter(right =>
            {
                if (mySignal == null) return string.Empty;
                var endTime = mySignal.GetDateTimeAtIndex(right).ToString("dd-MM-yyyy hh\\:mm\\:ss");
                return $"End Frequency: {endTime}";
            });
        }

        public void UpdateActiveSegment(int begin, int end)
        {

        }

        public void Reset(Signal newSignal)
        {
            mySegmentControl.IsEnabled = newSignal != null;

            if (newSignal != null)
            {
                mySignal = newSignal;
            }
        }

        public void AddChannel(Channel channel)
        {
            
        }

        private void ResetSegmentClick(object sender, RoutedEventArgs e)
        {
            
        }

        public void Dispose()
        {
            
        }

        private void ComboBoxMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void SelectInterval(object sender, RoutedEventArgs e)
        {
            
        }

        private void PreviewTextInputHandle(object sender, TextCompositionEventArgs e)
        {
            
        }

        private void PreviewPastingHandle(object sender, DataObjectPastingEventArgs e)
        {
            
        }

        private void ZeroMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }
        

        private void CountPerPage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            
        }
    }
}
