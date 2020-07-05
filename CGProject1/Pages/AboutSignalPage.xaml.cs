using System;
using System.Globalization;
using System.Windows.Controls;


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

        public AboutSignalPage() {
            InitializeComponent();

            InfoLabelInit(ref channelNumberText);
            InfoLabelInit(ref samplesNumberText);
            InfoLabelInit(ref samplingFrqText);
            InfoLabelInit(ref startDateTimeText);
            InfoLabelInit(ref endDateTimeText);
            InfoLabelInit(ref durationText);
            InfoLabelInit(ref activeSegmentText);
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

                ChannelsTable.ItemsSource = null;
                return;
            }

            channelNumberText.Content = signal.channels.Count;
            samplesNumberText.Content = signal.SamplesCount;
            samplingFrqText.Content = $"{signal.SamplingFrq} Гц (шаг между отсчетами {signal.DeltaTime} сек)";
            startDateTimeText.Content = signal.StartDateTime.ToString("dd-MM-yyyy hh\\:mm\\:ss\\.fff");
            endDateTimeText.Content = signal.EndTime.ToString("dd-MM-yyyy hh\\:mm\\:ss\\.fff");
            TimeSpan duration = signal.Duration;
            durationText.Content = $"{duration.Days} суток {duration.Hours} часов {duration.Minutes} минут {(duration.Seconds + (double)duration.Milliseconds / 1000).ToString("0.000", CultureInfo.InvariantCulture)} секунд";
            ChannelsTable.ItemsSource = signal.channels;
        }

        public void AddChannel(Channel channel) { }

        public void UpdateActiveSegment(int start, int end) {
            activeSegmentText.Content = $"Активный сегмент: [{start}; {end}]";
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
