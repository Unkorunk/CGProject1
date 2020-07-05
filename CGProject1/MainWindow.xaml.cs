using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CGProject1.Chart;
using CGProject1.SignalProcessing;
using Microsoft.Win32;
using FileFormats;

namespace CGProject1
{
    public partial class MainWindow : Window {
        public static MainWindow instance = null;

        private AboutSignal aboutSignalWindow;
        public Oscillograms oscillogramWindow;
        private ModelingWindow modelingWindow;
        private SaveWindow savingWindow;
        public AnalyzerWindow analyzerWindow;
        public SpectrogramWindow spectrogramWindow;

        public StatisticsWindow statisticsWindow;

        private bool isAboutSignalShowing = false;
        public bool isOscillogramShowing = false;
        private bool isModelingWindowShowing = false;
        private bool isSavingWindowShowing = false;
        public bool isAnalyzerShowing = false;
        public bool isSpectrogramsShowing = false;
        public bool isStatisticShowing = false;

        public Signal currentSignal;

        private List<ChartLine> charts = new List<ChartLine>();

        public MainWindow() {
            instance = this;
            InitializeComponent();

            this.Closed += (object sender, System.EventArgs e) => {
                CloseAll();

                Serializer.SerializeModels(Modelling.defaultPath, new List<ChannelConstructor>[] { Modelling.discreteModels, Modelling.continiousModels, Modelling.randomModels });
            };
        }

        private void CloseAll() {
            if (isAboutSignalShowing) {
                aboutSignalWindow.Close();
            }

            if (isModelingWindowShowing) {
                modelingWindow.Close();
            }

            if (isOscillogramShowing) {
                oscillogramWindow.Close();
            }

            if (isSavingWindowShowing) {
                savingWindow.Close();
            }

            if (isAnalyzerShowing) {
                analyzerWindow.Close();
            }
            if (isStatisticShowing) {
                statisticsWindow.Close();
            }
            if (isSpectrogramsShowing) {
                spectrogramWindow.Close();
            }
        }

        private void AboutClick(object sender, RoutedEventArgs e) {
            MessageBox.Show("Система визуализации и анализа многоканальных сигналов\r\n" +
                "Авторы:\r\n" +
                "Михалев Юрий (SmelJey)\r\n" +
                "Калинин Владислав (Unkorunk)\r\n" +
                "29.02.2020",
                "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ModelingClick(object sender, RoutedEventArgs e) {
            if (!this.isModelingWindowShowing) {
                this.modelingWindow = new ModelingWindow(currentSignal);
                this.modelingWindow.Closed += (object sender, EventArgs e) => this.isModelingWindowShowing = false;
                this.modelingWindow.Show();
                this.isModelingWindowShowing = true;
            } else {
                this.modelingWindow.Topmost = true;
                this.modelingWindow.Topmost = false;
            }
        }

        public void AddChannel(Channel channel) {
            channel.StartDateTime = this.currentSignal.StartDateTime;
            channel.SamplingFrq = this.currentSignal.SamplingFrq;

            this.currentSignal.channels.Add(channel);

            if (aboutSignalWindow != null) {
                aboutSignalWindow.Close();
            }

            if (modelingWindow != null) {
                modelingWindow.Close();
            }

            if (isSavingWindowShowing) {
                savingWindow.Close();
            }

            SetupChart(this.currentSignal.channels.Count - 1);
        }

        public void ResetSignal(Signal newSignal) {
            CloseAll();
 
            foreach (var chart in charts) {
                channels.Children.Remove(chart);
            }
            charts.Clear();

            Modelling.ResetCounters();

            this.currentSignal = newSignal;

            if (this.currentSignal == null) {
                return;
            }

            for (int i = 0; i < currentSignal.channels.Count; i++) {
                SetupChart(i);
            }
        }

        private void SetupChart(int i) {
            var chart = new ChartLine(currentSignal.channels[i]);
            chart.Height = 100;

            charts.Add(chart);
            channels.Children.Add(chart);

            chart.ContextMenu = new ContextMenu();

            var item1 = new MenuItem();
            item1.Header = "Осциллограмма";
            int cur = i;
            item1.Click += (object sender, RoutedEventArgs args) => {
                OpenOscillograms();

                oscillogramWindow.AddChannel(currentSignal.channels[cur]);
            };
            chart.ContextMenu.Items.Add(item1);

            var item2 = new MenuItem();
            item2.Header = "Статистика";
            item2.Click += (object sender, RoutedEventArgs args) => {
                if (!isStatisticShowing) {
                    statisticsWindow = new StatisticsWindow();
                    isStatisticShowing = true;
                    statisticsWindow.Closed += (object sender, EventArgs e) => isStatisticShowing = false;
                    statisticsWindow.Show();
                }

                statisticsWindow.Update(charts[cur], true);
            };
            chart.ContextMenu.Items.Add(item2);

            var item3 = new MenuItem();
            item3.Header = "Анализ";
            item3.Click += (object sender, RoutedEventArgs args) => {
                OpenAnalyzer();
                analyzerWindow.AddChannel(currentSignal.channels[cur]);
            };
            chart.ContextMenu.Items.Add(item3);

            var item4 = new MenuItem();
            item4.Header = "Спектрограмма";
            item4.Click += (object sender, RoutedEventArgs args) => {
                OpenSpectrograms();
                spectrogramWindow.AddChannel(currentSignal.channels[cur]);
            };
            chart.ContextMenu.Items.Add(item4);

            chart.Begin = 0;
            chart.End = currentSignal.SamplesCount;
        }

        private void OpenFileClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog() {
                Filter = "txt files (*.txt)|*.txt|wave files (*.wav;*.wave)|*.wav;*.wave|dat files (*.dat)|*.dat|mp3 files (*.mp3)|*.mp3"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                IReader reader;
                switch (Path.GetExtension(openFileDialog.FileName))
                {
                    case ".mp3":
                        reader = new MP3Reader();
                        break;
                    case ".dat":
                        reader = new DatReader();
                        break;
                    case ".wav":
                    case ".wave":
                        reader = new WaveReader();
                        break;
                    case ".txt":
                        reader = new TxtReader();
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (reader.TryRead(File.ReadAllBytes(openFileDialog.FileName), out var waveFile))
                {
                    var signal = new Signal(Path.GetFileName(openFileDialog.FileName));
                    signal.SamplingFrq = waveFile.nSamplesPerSec;
                    signal.StartDateTime = waveFile.dateTime;

                    for (int i = 0; i < waveFile.nChannels; i++)
                    {
                        signal.channels.Add(new Channel(waveFile.data.GetLength(0)));
                        signal.channels[i].Source = signal.fileName;
                        signal.channels[i].Name = waveFile.channelNames[i] ?? ("Channel " + i);
                        for (int j = 0; j < waveFile.data.GetLength(0); j++)
                        {
                            signal.channels[i].values[j] = waveFile.data[j, i];
                        }
                    }

                    signal.UpdateChannelsInfo();

                    ResetSignal(signal);
                }
            }
        }

        private void OnChannelClick(object sender, MouseButtonEventArgs e) {
            var point = Mouse.GetPosition(channels);

            int row = 0;
            double accumulatedHeight = 0.0;

            foreach (var chart in charts) {
                accumulatedHeight += chart.ActualHeight;
                if (accumulatedHeight >= point.Y)
                    break;
                row++;
            }

            if (currentSignal == null || row >= currentSignal.channels.Count) {
                return;
            }

            foreach (var chart in charts) {
                chart.Selected = false;
                chart.InvalidateVisual();
            }

            charts[row].Selected = true;
            charts[row].InvalidateVisual();
        }

        private void AboutSignalClick(object sender, RoutedEventArgs e) {
            if (!this.isAboutSignalShowing)
            {
                aboutSignalWindow = new AboutSignal();
                aboutSignalWindow.UpdateInfo(currentSignal);
                aboutSignalWindow.Closed += (object sender, System.EventArgs e) => this.isAboutSignalShowing = false;
                aboutSignalWindow.Show();
                isAboutSignalShowing = true;
            } else {
                aboutSignalWindow.Topmost = true;
                aboutSignalWindow.Topmost = false;
            }
        }

        private void OscillogramsClick(object sender, RoutedEventArgs e) {
            OpenOscillograms();
        }

        private void AnalyzatorClick(object sender, RoutedEventArgs e) {
            OpenAnalyzer();
        }

        private void SpectrogramsClick(object sender, RoutedEventArgs e) {
            OpenSpectrograms();
        }

        private void OpenOscillograms() {
            if (!this.isOscillogramShowing) {
                isOscillogramShowing = true;
                oscillogramWindow = new Oscillograms();
                oscillogramWindow.Closed += (object sender, System.EventArgs e) => this.isOscillogramShowing = false;
                oscillogramWindow.Update(currentSignal);
                oscillogramWindow.Show();
            } else {
                oscillogramWindow.Topmost = true;
                oscillogramWindow.Topmost = false;
            }
        }

        private void OpenAnalyzer() {
            if (!this.isAnalyzerShowing) {
                isAnalyzerShowing = true;

                int begin = 0;
                int end = 0;

                if (this.currentSignal != null) {
                    end = this.currentSignal.SamplesCount - 1;
                }

                if (isOscillogramShowing) {
                    begin = oscillogramWindow.GetBegin();
                    end = oscillogramWindow.GetEnd();
                }

                analyzerWindow = new AnalyzerWindow(begin, end);
                analyzerWindow.Closed += (object sender, System.EventArgs e) => this.isAnalyzerShowing = false;
                analyzerWindow.Show();
            } else {
                analyzerWindow.Topmost = true;
                analyzerWindow.Topmost = false;
            }
        }

        private void OpenSpectrograms() {
            if (!this.isSpectrogramsShowing) {
                isSpectrogramsShowing = true;
                spectrogramWindow = new SpectrogramWindow();
                spectrogramWindow.Closed += (object sender, System.EventArgs e) => this.isSpectrogramsShowing = false;
                spectrogramWindow.Show();
            } else {
                spectrogramWindow.Topmost = true;
                spectrogramWindow.Topmost = false;
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e) {
            if (this.currentSignal == null) {
                MessageBox.Show("Нет сигнала для сохранения", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!this.isSavingWindowShowing) {
                int begin = 0;
                int end = this.currentSignal.SamplesCount - 1;

                if (oscillogramWindow != null && isOscillogramShowing) {
                    begin = oscillogramWindow.GetBegin();
                    end = oscillogramWindow.GetEnd();
                }

                savingWindow = new SaveWindow(this.currentSignal, begin, end);
                savingWindow.Closed += (object sender, System.EventArgs e) => this.isSavingWindowShowing = false;
                savingWindow.Show();
                this.isSavingWindowShowing = true;
            } else {
                savingWindow.Topmost = true;
                savingWindow.Topmost = false;
            }
        }
    }
}
