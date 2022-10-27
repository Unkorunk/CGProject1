using System.IO;
using System.Windows;
using System.Windows.Controls;
using CGProject1.FileFormat;
using CGProject1.SignalProcessing;
using NAudio.Wave;

namespace CGProject1.Pages
{
    public partial class MicrophonePage : Page, IChannelComponent
    {
        private MemoryStream memoryStream;
        private WaveFileWriter waveFileWriter;

        private bool started;
        private WaveIn waveIn;

        public MicrophonePage()
        {
            InitializeComponent();

            for (var i = 0; i < WaveIn.DeviceCount; i++)
            {
                DeviceComboBox.Items.Add(WaveIn.GetCapabilities(i).ProductName);
            }

            DeviceComboBox.SelectedIndex = DeviceComboBox.Items.Count != 0 ? 0 : -1;
        }

        private void RecordButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                started = !started;
                button.Content = started ? "Stop Recording" : "Start Recording";

                if (started)
                {
                    Start();
                }
                else
                {
                    End();
                }
            }
        }

        private void Start()
        {
            var deviceIdx = DeviceComboBox.SelectedIndex;

            if (int.TryParse(SampleRateTextBox.Text, out var sampleRate) && deviceIdx >= 0 &&
                deviceIdx < WaveIn.DeviceCount)
            {
                memoryStream = new MemoryStream();

                waveIn = new WaveIn
                {
                    DeviceNumber = deviceIdx,
                    WaveFormat = new WaveFormat(sampleRate, 16, WaveIn.GetCapabilities(deviceIdx).Channels)
                };

                memoryStream = new MemoryStream();
                waveFileWriter = new WaveFileWriter(memoryStream, waveIn.WaveFormat);

                waveIn.DataAvailable += (sender, args) =>
                {
                    waveFileWriter.Write(args.Buffer, 0, args.BytesRecorded);
                    waveFileWriter.Flush();
                };

                waveIn.StartRecording();
            }
        }

        private void End()
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;

                var waveReader = new Mp3Reader {IsMp3FileReader = false};
                if (waveReader.TryRead(memoryStream.GetBuffer(), out var fileInfo))
                {
                    var signal = new Signal("Microphone Signal");
                    signal.SamplingFrq = fileInfo.nSamplesPerSec;
                    signal.StartDateTime = fileInfo.dateTime;

                    for (int i = 0; i < fileInfo.nChannels; i++)
                    {
                        signal.channels.Add(new Channel(fileInfo.data.GetLength(0)));
                        signal.channels[i].Source = signal.fileName;
                        signal.channels[i].Name = fileInfo.channelNames[i] ?? ("Channel " + i);
                        for (int j = 0; j < fileInfo.data.GetLength(0); j++)
                        {
                            signal.channels[i].values[j] = fileInfo.data[j, i];
                        }
                    }
                    
                    signal.UpdateChannelsInfo();

                    MainWindow.Instance.ResetSignal(signal);
                }

                waveFileWriter.Dispose();
                memoryStream.Close();
            }
        }

        public void Reset(Signal signal)
        {
        }

        public void AddChannel(Channel channel)
        {
        }

        public void UpdateActiveSegment(int start, int end)
        {
        }
    }
}