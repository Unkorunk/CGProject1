using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CGProject1.FileFormat;
using CGProject1.SignalProcessing;
using Microsoft.Win32;
using FileInfo = CGProject1.FileFormat.FileInfo;

namespace CGProject1 {
    public partial class SaveWindow : Window {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        
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

            Logger.Info($"Tried to save signal {signalToSave}");

            if (signalToSave.channels.Count == 0) {
                MessageBox.Show("Не выбрано ни одного канала", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var saveDialog = new SaveFileDialog();
            saveDialog.Filter = "txt files (.txt)|*.txt|wave files (*.wav;*.wave)|*.wav;*.wave|dat files (*.dat)|*.dat";
            if (saveDialog.ShowDialog() == true) {
                IWriter writer;
                switch (Path.GetExtension(saveDialog.FileName))
                {
                    case ".txt":
                        writer = new TxtWriter();
                        break;
                    case ".wav":
                    case ".wave":
                        writer = new WaveWriter();
                        break;
                    case ".dat":
                        writer = new DatWriter();
                        break;
                    default: throw new NotImplementedException();
                }

                File.WriteAllBytes(saveDialog.FileName,
                    writer.TryWrite(SignalToFileInfo(signalToSave, begin, end)));
            }
        }

        private FileInfo SignalToFileInfo(Signal signal, int begin, int end)
        {
            var fileInfo = new FileInfo
            {
                nChannels = signal.channels.Count,
                nSamplesPerSec = signal.SamplingFrq,
                dateTime = signal.StartDateTime + TimeSpan.FromSeconds(begin * signal.DeltaTime)
            };

            fileInfo.channelNames = new string[fileInfo.nChannels];
            for (int i = 0; i < fileInfo.nChannels; i++)
            {
                fileInfo.channelNames[i] = signal.channels[i].Name;
            }

            fileInfo.data = new double[end - begin + 1, fileInfo.nChannels];
            for (int i = 0; i < fileInfo.data.GetLength(0); i++)
            {
                for (int j = 0; j < fileInfo.data.GetLength(1); j++)
                {
                    fileInfo.data[i, j] = signal.channels[j].values[begin + i];
                }
            }

            return fileInfo;
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
