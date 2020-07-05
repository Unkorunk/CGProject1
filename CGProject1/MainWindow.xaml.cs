using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CGProject1.Chart;
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

        private IPageComponent[] pages;

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
            var statisticFrame = new Frame();
            statisticFrame.Navigate(statisticsPage);

            statisticsPane = new LayoutAnchorable();
            statisticsPane.Title = "Статистики";
            statisticsPane.Content = statisticFrame;
            
            RightPane.Children.Add(statisticsPane);


            channelsPage = new ChannelsPage();
            var channelsFrame = new Frame();
            channelsFrame.Navigate(channelsPage);

            channelsPane = new LayoutAnchorable();
            channelsPane.Title = "Каналы";
            channelsPane.Content = channelsFrame;

            LeftPane.Children.Add(channelsPane);


            oscillogramsPage = new OscillogramsPage();
            var oscillogramsFrame = new Frame();
            oscillogramsFrame.Navigate(oscillogramsPage);

            oscillogramsPane = new LayoutAnchorable();
            oscillogramsPane.Title = "Осциллограммы";
            oscillogramsPane.Content = oscillogramsFrame;

            LowerMiddlePane.Children.Add(oscillogramsPane);


            analyzerPage = new AnalyzerPage();
            var analyzerFrame = new Frame();
            analyzerFrame.Navigate(analyzerPage);

            analyzerPane = new LayoutAnchorable();
            analyzerPane.Title = "Анализ Фурье";
            analyzerPane.Content = analyzerFrame;

            UpperMiddlePane.Children.Add(analyzerPane);


            spectrogramsPage = new SpectrogramsPage();
            var spectrogramsFrame = new Frame();
            spectrogramsFrame.Navigate(spectrogramsPage);

            spectrogramsPane = new LayoutAnchorable();
            spectrogramsPane.Title = "Спектрограммы";
            spectrogramsPane.Content = spectrogramsFrame;

            UpperMiddlePane.Children.Add(spectrogramsPane);


            aboutSignalPage = new AboutSignalPage();
            var aboutSignalFrame = new Frame();
            aboutSignalFrame.Navigate(aboutSignalPage);

            aboutSignalPane = new LayoutAnchorable();
            aboutSignalPane.Title = "О сигнале";
            aboutSignalPane.Content = aboutSignalFrame;

            RightPane.Children.Add(aboutSignalPane);

            pages = new IPageComponent[] { channelsPage, aboutSignalPage, statisticsPage, oscillogramsPage, analyzerPage, spectrogramsPage };
        }

        public void AddStatistics(Channel channel) {
            statisticsPage.AddChannel(channel);
            statisticsPane.Show();
        }

        public void AddOscillogram(Channel channel) {
            oscillogramsPage.AddChannel(channel);
            oscillogramsPane.Show();
        }

        public void AddAnalyze(Channel channel) {
            analyzerPage.AddChannel(channel);
            analyzerPane.Show();
        }

        public void AddSpectrogram(Channel channel) {
            spectrogramsPage.AddChannel(channel);
            spectrogramsPane.Show();
        }

        public void UpdateActiveSegment(int begin, int end) {
            this.begin = begin;
            this.end = end;

            foreach (var page in pages) {
                page.UpdateActiveSegment(begin, end);
            }
        }

        private void OpenStatisticsPage(object sender, RoutedEventArgs e) {
            statisticsPane.Show();
        }

        private void OpenAnalyzerPage(object sender, RoutedEventArgs e) {
            analyzerPane.Show();
        }

        private void OpenOscillogramsPage(object sender, RoutedEventArgs e) {
            oscillogramsPane.Show();
        }

        private void OpenSpectrogramsPage(object sender, RoutedEventArgs e) {
            spectrogramsPane.Show();
        }

        private void OpenAboutSignalPage(object sender, RoutedEventArgs e) {
            aboutSignalPane.Show();
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
