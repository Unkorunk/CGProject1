using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CGProject1.Pages;
using CGProject1.SignalProcessing;
using Microsoft.Win32;
using WAVE;

using Xceed.Wpf.AvalonDock.Layout;


namespace CGProject1
{
    public partial class MainWindow : Window {
        public static MainWindow instance = null;

        #region(DEPRECATED)
        private ModelingWindow modelingWindow;
        private SaveWindow savingWindow;

        private bool isModelingWindowShowing = false;
        private bool isSavingWindowShowing = false;
        #endregion

        private StatisticsPage statisticsPage = null;
        private LayoutAnchorable statisticsPane = null;

        private ChannelsPage channelsPage = null;
        private LayoutAnchorable channelsPane = null;

        private OscillogramsPage oscillogramsPage = null;
        private LayoutAnchorable oscillogramsPane = null;

        private AnalyzerPage analyzerPage = null;
        private LayoutAnchorable analyzerPane = null;

        private SpectrogramsPage spectrogramsPage = null;
        private LayoutAnchorable spectrogramsPane = null;

        private AboutSignalPage aboutSignalPage = null;
        private LayoutAnchorable aboutSignalPane = null;

        private IChannelComponent[] pages;
        private LayoutAnchorable[] panes;

        public Signal currentSignal;

        private int begin;
        private int end;

        public MainWindow() {
            instance = this;
            InitializeComponent();

            this.Closed += (object sender, EventArgs e) => {
                CloseAll();

                Serializer.SerializeModels(Modelling.defaultPath, new List<ChannelConstructor>[] { Modelling.discreteModels, Modelling.continiousModels, Modelling.randomModels });
            };

            statisticsPage = new StatisticsPage();
            statisticsPane = new LayoutAnchorable();
            statisticsPane.Title = "Статистики";
            RightPane.Children.Add(statisticsPane);

            channelsPage = new ChannelsPage();
            channelsPane = new LayoutAnchorable();
            channelsPane.Title = "Каналы";
            LeftPane.Children.Add(channelsPane);

            oscillogramsPage = new OscillogramsPage();
            oscillogramsPane = new LayoutAnchorable();
            oscillogramsPane.Title = "Осциллограммы";
            LowerMiddlePane.Children.Add(oscillogramsPane);

            analyzerPage = new AnalyzerPage();
            analyzerPane = new LayoutAnchorable();
            analyzerPane.Title = "Анализ Фурье";
            UpperMiddlePane.Children.Add(analyzerPane);

            spectrogramsPage = new SpectrogramsPage();
            spectrogramsPane = new LayoutAnchorable();
            spectrogramsPane.Title = "Спектрограммы";
            UpperMiddlePane.Children.Add(spectrogramsPane);

            aboutSignalPage = new AboutSignalPage();
            aboutSignalPane = new LayoutAnchorable();
            aboutSignalPane.Title = "О сигнале";
            RightPane.Children.Add(aboutSignalPane);

            pages = new IChannelComponent[] { channelsPage, aboutSignalPage, statisticsPage, oscillogramsPage, analyzerPage, spectrogramsPage };
            panes = new LayoutAnchorable[] { channelsPane, aboutSignalPane, statisticsPane, oscillogramsPane, analyzerPane, spectrogramsPane };

            if (pages.Length != panes.Length) {
                throw new InvalidDataException("Invalid count of panes / pages");
            }

            for (int i = 0; i < panes.Length; i++) {
                var frame = new Frame();
                frame.Navigate(pages[i]);
                panes[i].Content = frame;
            }
        }

        public void AddStatistics(Channel channel) {
            statisticsPage.AddChannel(channel);
            OpenPane(statisticsPane);
        }

        public void AddOscillogram(Channel channel) {
            oscillogramsPage.AddChannel(channel);
            OpenPane(oscillogramsPane);
        }

        public void AddAnalyze(Channel channel) {
            analyzerPage.AddChannel(channel);
            OpenPane(analyzerPane);
        }

        public void AddSpectrogram(Channel channel) {
            spectrogramsPage.AddChannel(channel);
            OpenPane(spectrogramsPane);
        }

        public void UpdateActiveSegment(int begin, int end) {
            this.begin = begin;
            this.end = end;

            foreach (var page in pages) {
                page.UpdateActiveSegment(begin, end);
            }
        }

        public void ResetSignal(Signal newSignal) {
            CloseAll();

            foreach (var page in pages) {
                page.Reset(newSignal);
            }

            Modelling.ResetCounters();

            this.currentSignal = newSignal;
            oscillogramsPage.Reset(newSignal);
            UpdateActiveSegment(0, newSignal.SamplesCount - 1);

            if (this.currentSignal == null) {
                return;
            }

            for (int i = 0; i < currentSignal.channels.Count; i++) {
                channelsPage.AddChannel(currentSignal.channels[i]);
            }
        }

        private void OpenPane(LayoutAnchorable pane) {
            pane.Show();
            pane.IsSelected = true;
            pane.IsActive = true;
        }

        private void OpenStatisticsPage(object sender, RoutedEventArgs e) {
            OpenPane(statisticsPane);
        }

        private void OpenAnalyzerPage(object sender, RoutedEventArgs e) {
            OpenPane(analyzerPane);
        }

        private void OpenOscillogramsPage(object sender, RoutedEventArgs e) {
            OpenPane(oscillogramsPane);
            
        }

        private void OpenSpectrogramsPage(object sender, RoutedEventArgs e) {
            OpenPane(spectrogramsPane);
        }

        private void OpenAboutSignalPage(object sender, RoutedEventArgs e) {
            OpenPane(aboutSignalPane);
        }

        private void OpenChannelsPage(object sender, RoutedEventArgs e) {
            OpenPane(channelsPane);
        }

        private void CloseAll() {
            if (isModelingWindowShowing) {
                modelingWindow.Close();
            }

            if (isSavingWindowShowing) {
                savingWindow.Close();
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

            if (modelingWindow != null) {
                modelingWindow.Close();
            }

            if (isSavingWindowShowing) {
                savingWindow.Close();
            }

            channelsPage.AddChannel(channel);
        }

        private void OpenFileClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog() { Filter = "txt files (*.txt)|*.txt|wave files (*.wav;*.wave)|*.wav;*.wave" };

            if (openFileDialog.ShowDialog() == true)
            {
                switch(Path.GetExtension(openFileDialog.FileName))
                {
                    case ".wav":
                    case ".wave":
                        if (WaveReader.TryRead(File.ReadAllBytes(openFileDialog.FileName), out var waveFile))
                        {
                            var signal = new Signal(Path.GetFileName(openFileDialog.FileName));
                            signal.SamplingFrq = waveFile.nSamplesPerSec;

                            for (int i = 0; i < waveFile.nChannels; i++)
                            {
                                signal.channels.Add(new Channel(waveFile.data.GetLength(0)));
                                signal.channels[i].Source = signal.fileName;
                                signal.channels[i].Name = signal.fileName + "_" + i;
                                for (int j = 0; j < waveFile.data.GetLength(0); j++)
                                {
                                    signal.channels[i].values[j] = waveFile.data[j, i];
                                }
                            }

                            signal.UpdateChannelsInfo();

                            ResetSignal(signal);
                        }
                        break;
                    case ".txt":
                        ResetSignal(Parser.Parse(openFileDialog.FileName));
                        break;
                    default: throw new NotImplementedException();
                }
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e) {
            if (this.currentSignal == null) {
                MessageBox.Show("Нет сигнала для сохранения", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!this.isSavingWindowShowing) {
                savingWindow = new SaveWindow(this.currentSignal, this.begin, this.end);
                savingWindow.Closed += (object sender, EventArgs e) => this.isSavingWindowShowing = false;
                savingWindow.Show();
                this.isSavingWindowShowing = true;
            } else {
                savingWindow.Topmost = true;
                savingWindow.Topmost = false;
            }
        }
    }
}
