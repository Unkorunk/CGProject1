using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace CGProject1 {
    public partial class SaveWindow : Window {
        public SaveWindow(Signal signal, int begin, int end) {
            InitializeComponent();

            this.currentSignal = signal;
            this.checkers = new CheckBox[signal.channels.Count];

            for (int i = 0; i < signal.channels.Count; i++) {
                var row = new RowDefinition();
                row.Height = new GridLength(25);
                ChannelsGrid.RowDefinitions.Add(row);

                var channel = signal.channels[i];

                var channelLabel = new Label();
                channelLabel.Content = channel.Name;
                Grid.SetColumn(channelLabel, 0);
                Grid.SetRow(channelLabel, i + 1);
                ChannelsGrid.Children.Add(channelLabel);

                var channelCheckBox = new CheckBox();
                channelCheckBox.IsChecked = true;
                checkers[i] = channelCheckBox;
                Grid.SetColumn(channelCheckBox, 1);
                Grid.SetRow(channelCheckBox, i + 1);
                ChannelsGrid.Children.Add(channelCheckBox);
            }

            BeginField.Text = begin.ToString();
            EndField.Text = end.ToString();
        }

        private Signal currentSignal;
        private CheckBox[] checkers;

        private void FullSignalClick(object sender, RoutedEventArgs e) {
            BeginField.Text = "0";
            EndField.Text = (this.currentSignal.SamplesCount - 1).ToString();
        }

        private void SaveClick(object sender, RoutedEventArgs e) {
            int begin = int.Parse(BeginField.Text);
            int end = int.Parse(EndField.Text);

            if (begin < 0) {
                begin = 0;
            }

            if(begin >= this.currentSignal.SamplesCount) {
                begin = this.currentSignal.SamplesCount - 1;
            }

            if (end < begin) {
                end = begin;
            }

            if (end >= this.currentSignal.SamplesCount) {
                end = this.currentSignal.SamplesCount - 1;
            }

            BeginField.Text = begin.ToString();
            EndField.Text = end.ToString();

            var signalToSave = new Signal(this.currentSignal.fileName);
            signalToSave.StartDateTime = this.currentSignal.StartDateTime;
            signalToSave.SamplingFrq = this.currentSignal.SamplingFrq;

            for (int i = 0; i < this.currentSignal.channels.Count; i++) {
                if (checkers[i].IsChecked == true) {
                    signalToSave.channels.Add(this.currentSignal.channels[i]);
                }
            }

            if (signalToSave.channels.Count == 0) {
                MessageBox.Show("Не выбрано ни одного канала", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Text file (.txt)|*.txt";
            if (saveDialog.ShowDialog() == true) {
                SignalProcessing.Serializer.Serialize(saveDialog.FileName, signalToSave, begin, end);
            }
        }

        private void previewTextInput(object sender, TextCompositionEventArgs e) {
            e.Handled = !TextIsNumeric(e.Text);
        }

        private void previewPasting(object sender, DataObjectPastingEventArgs e) {
            if (e.DataObject.GetDataPresent(typeof(string))) {
                string input = (string)e.DataObject.GetData(typeof(string));
                if (!TextIsNumeric(input)) e.CancelCommand();
            } else {
                e.CancelCommand();
            }
        }

        private bool TextIsNumeric(string input) {
            return input.All(c => char.IsDigit(c) || char.IsControl(c));
        }
    }
}
