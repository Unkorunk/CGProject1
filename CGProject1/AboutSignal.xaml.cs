using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace CGProject1 {
    public partial class AboutSignal : Window {
        private int curRow = 1;

        private Label channelNumberText;
        private Label samplesNumberText;
        private Label samplingFrqText;
        private Label startDateTimeText;
        private Label endDateTimeText;
        private Label durationText;

        public AboutSignal() {
            InitializeComponent();

            InfoLabelInit(ref channelNumberText);
            InfoLabelInit(ref samplesNumberText);
            InfoLabelInit(ref samplingFrqText);
            InfoLabelInit(ref startDateTimeText);
            InfoLabelInit(ref endDateTimeText);
            InfoLabelInit(ref durationText);
        }

        public void UpdateInfo(Signal signal) {
            if (signal == null) {
                channelNumberText.Content = "No signal";
                samplesNumberText.Content = "No signal";
                samplingFrqText.Content = "No signal";
                startDateTimeText.Content = "No signal";
                endDateTimeText.Content = "No signal";
                durationText.Content = "No signal";

                ChannelsTable.ItemsSource = null;
                return;
            }

            channelNumberText.Content = signal.channels.Length;
            samplesNumberText.Content = signal.SamplesCount;
            samplingFrqText.Content = $"{signal.samplingFrq} Гц (шаг между отсчетами {signal.DeltaTime} сек)";
            startDateTimeText.Content = signal.startDateTime.ToString("dd-MM-yyyy hh\\:mm\\:ss\\.fff");
            endDateTimeText.Content = signal.EndTime.ToString("dd-MM-yyyy hh\\:mm\\:ss\\.fff");
            TimeSpan duration = signal.Duration;
            durationText.Content = $"{duration.Days} суток {duration.Hours} часов {duration.Minutes} минут {(duration.Seconds + (double)duration.Milliseconds / 1000).ToString("0.000", CultureInfo.InvariantCulture)} секунд";
            ChannelsTable.ItemsSource = signal.channels;
        }

        private void InfoLabelInit(ref Label label) {
            label = new Label();
            MainGrid.Children.Add(label);
            label.SetValue(Grid.ColumnProperty, 1);
            label.SetValue(Grid.RowProperty, curRow);
            curRow++;
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            e.Cancel = true;  // cancels the window close    
            this.Hide();      // Programmatically hides the window
        }
    }
}
