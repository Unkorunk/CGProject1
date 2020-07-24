using System;
using System.Globalization;
using System.Windows.Controls;
using CGProject1.SignalProcessing;


namespace CGProject1.Pages {
    public partial class AboutSignalPage : Page, IChannelComponent {
        private int curRow = 1;

        private Label channelNumberText;
        private Label samplesNumberText;
        private Label samplingFrqText;
        private Label startDateTimeText;
        private Label endDateTimeText;
        private Label durationText;
        private Label activeSegmentText;
        private Label activeSegmentLengthText;

        public AboutSignalPage() {
            InitializeComponent();

            InfoLabelInit(ref channelNumberText);
            InfoLabelInit(ref samplesNumberText);
            InfoLabelInit(ref samplingFrqText);
            InfoLabelInit(ref startDateTimeText);
            InfoLabelInit(ref endDateTimeText);
            InfoLabelInit(ref durationText);
            InfoLabelInit(ref activeSegmentText);
            InfoLabelInit(ref activeSegmentLengthText);
        }

        public void Reset(Signal signal) {
            if (signal == null) {
                channelNumberText.Content = "No signal";
                samplesNumberText.Content = "No signal";
                samplingFrqText.Content = "No signal";
                startDateTimeText.Content = "No signal";
                endDateTimeText.Content = "No signal";
                durationText.Content = "No signal";
                activeSegmentText.Content = "No signal";
                activeSegmentLengthText.Content = "No signal";

                ChannelsTable.ItemsSource = null;
                return;
            }

            channelNumberText.Content = signal.channels.Count;
            samplesNumberText.Content = signal.SamplesCount;
            samplingFrqText.Content = $"{signal.SamplingFrq} Гц (шаг между отсчетами {signal.DeltaTime} сек)";
            startDateTimeText.Content = signal.StartDateTime.ToString("dd-MM-yyyy HH\\:mm\\:ss\\.fff", CultureInfo.InvariantCulture);
            endDateTimeText.Content = signal.EndTime.ToString("dd-MM-yyyy HH\\:mm\\:ss\\.fff", CultureInfo.InvariantCulture);
            TimeSpan duration = signal.Duration;
            durationText.Content = $"{duration.Days} суток {duration.Hours} часов {duration.Minutes} минут {(duration.Seconds + (double)duration.Milliseconds / 1000).ToString("0.000", CultureInfo.InvariantCulture)} секунд";
            ChannelsTable.ItemsSource = signal.channels;
        }

        public void AddChannel(Channel channel) { }

        public void UpdateActiveSegment(int start, int end) {
            int fragmentLen = end - start + 1;

            activeSegmentText.Content = $"[{start}; {end}] ({fragmentLen} отсчетов)";

            if (MainWindow.Instance.currentSignal == null) {
                activeSegmentLengthText.Content = $"0 ({fragmentLen})";
            } else {
                TimeSpan fragmentDuration = TimeSpan.FromSeconds(MainWindow.Instance.currentSignal.DeltaTime * fragmentLen);
                activeSegmentLengthText.Content = $"{fragmentDuration.Days} суток {fragmentDuration.Hours} часов {fragmentDuration.Minutes} минут {(fragmentDuration.Seconds + (double)fragmentDuration.Milliseconds / 1000).ToString("0.000", CultureInfo.InvariantCulture)} секунд";
            }
        }

        private void InfoLabelInit(ref Label label) {
            label = new Label();
            MainGrid.Children.Add(label);
            label.SetValue(Grid.ColumnProperty, 1);
            label.SetValue(Grid.RowProperty, curRow);
            curRow++;
        }
    }
}
