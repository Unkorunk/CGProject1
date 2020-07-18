using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CGProject1.Pages;
using CGProject1.SignalProcessing;
using Microsoft.Win32;
using FileFormats;

using Xceed.Wpf.AvalonDock.Layout;


namespace CGProject1
{
    public partial class MainWindow : Window
    {
        public static MainWindow instance = null;

        private ModelingWindow modelingWindow;
        private SaveWindow savingWindow;

        private bool isModelingWindowShowing = false;
        private bool isSavingWindowShowing = false;

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

        public MainWindow()
        {
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
            UpperMiddlePane.Children.Add(oscillogramsPane);

            analyzerPage = new AnalyzerPage();
            analyzerPane = new LayoutAnchorable();
            analyzerPane.Title = "Анализ Фурье";
            LowerMiddlePane.Children.Add(analyzerPane);

            spectrogramsPage = new SpectrogramsPage();
            spectrogramsPane = new LayoutAnchorable();
            spectrogramsPane.Title = "Спектрограммы";
            LowerMiddlePane.Children.Add(spectrogramsPane);

            aboutSignalPage = new AboutSignalPage();
            aboutSignalPane = new LayoutAnchorable();
            aboutSignalPane.Title = "О сигнале";
            RightPane.Children.Add(aboutSignalPane);

            pages = new IChannelComponent[] { channelsPage, aboutSignalPage, statisticsPage, oscillogramsPage, analyzerPage, spectrogramsPage };
            panes = new LayoutAnchorable[] { channelsPane, aboutSignalPane, statisticsPane, oscillogramsPane, analyzerPane, spectrogramsPane };

            if (pages.Length != panes.Length)
            {
                throw new InvalidDataException("Invalid count of panes / pages");
            }

            for (int i = 0; i < panes.Length; i++)
            {
                var frame = new Frame();
                frame.Navigate(pages[i]);
                panes[i].Content = frame;
            }

            ResetSignal(null);
        }

        public void AddStatistics(Channel channel)
        {
            statisticsPage.AddChannel(channel);
            OpenPane(statisticsPane);
        }

        public void AddOscillogram(Channel channel)
        {
            oscillogramsPage.AddChannel(channel);
            OpenPane(oscillogramsPane);
        }

        public void AddAnalyze(Channel channel)
        {
            analyzerPage.AddChannel(channel);
            OpenPane(analyzerPane);
        }

        public void AddSpectrogram(Channel channel)
        {
            spectrogramsPage.AddChannel(channel);
            OpenPane(spectrogramsPane);
        }

        public void UpdateActiveSegment(int begin, int end)
        {
            this.begin = begin;
            this.end = end;

            foreach (var page in pages)
            {
                page.UpdateActiveSegment(begin, end);
            }
        }

        public void ResetSignal(Signal newSignal)
        {
            CloseAll();

            foreach (var page in pages)
            {
                page.Reset(newSignal);
            }

            Modelling.ResetCounters();

            this.currentSignal = newSignal;
            oscillogramsPage.Reset(newSignal);

            if (this.currentSignal == null)
            {
                return;
            }

            UpdateActiveSegment(0, newSignal.SamplesCount - 1);

            for (int i = 0; i < currentSignal.channels.Count; i++)
            {
                channelsPage.AddChannel(currentSignal.channels[i]);
            }
        }

        private void OpenPane(LayoutAnchorable pane)
        {
            pane.Show();
            pane.IsSelected = true;
        }

        private void OpenStatisticsPage(object sender, RoutedEventArgs e)
        {
            OpenPane(statisticsPane);
        }

        private void OpenAnalyzerPage(object sender, RoutedEventArgs e)
        {
            OpenPane(analyzerPane);
        }

        private void OpenOscillogramsPage(object sender, RoutedEventArgs e)
        {
            OpenPane(oscillogramsPane);

        }

        private void OpenSpectrogramsPage(object sender, RoutedEventArgs e)
        {
            OpenPane(spectrogramsPane);
        }

        private void OpenAboutSignalPage(object sender, RoutedEventArgs e)
        {
            OpenPane(aboutSignalPane);
        }

        private void OpenChannelsPage(object sender, RoutedEventArgs e)
        {
            OpenPane(channelsPane);
        }

        private void CloseAll()
        {
            if (isModelingWindowShowing)
            {
                modelingWindow.Close();
            }

            if (isSavingWindowShowing)
            {
                savingWindow.Close();
            }
        }

        private void AboutClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Система визуализации и анализа многоканальных сигналов\r\n" +
                "Авторы:\r\n" +
                "Михалев Юрий (SmelJey)\r\n" +
                "Калинин Владислав (Unkorunk)\r\n" +
                "29.02.2020",
                "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ModelingClick(object sender, RoutedEventArgs e)
        {
            if (!this.isModelingWindowShowing)
            {
                this.modelingWindow = new ModelingWindow(currentSignal);
                this.modelingWindow.Closed += (object sender, EventArgs e) => this.isModelingWindowShowing = false;
                this.modelingWindow.Show();
                this.isModelingWindowShowing = true;
            }
            else
            {
                this.modelingWindow.Topmost = true;
                this.modelingWindow.Topmost = false;
            }
        }

        public void AddChannel(Channel channel)
        {
            channel.StartDateTime = this.currentSignal.StartDateTime;
            channel.SamplingFrq = this.currentSignal.SamplingFrq;

            this.currentSignal.channels.Add(channel);

            if (modelingWindow != null)
            {
                modelingWindow.Close();
            }

            if (isSavingWindowShowing)
            {
                savingWindow.Close();
            }

            channelsPage.AddChannel(channel);
        }

        private void OpenFileClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
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
                        reader = new DATReader();
                        break;
                    case ".wav":
                    case ".wave":
                        reader = new WAVEReader();
                        break;
                    case ".txt":
                        reader = new TXTReader();
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (reader.TryRead(File.ReadAllBytes(openFileDialog.FileName), out var fileInfo))
                {
                    var signal = new Signal(Path.GetFileName(openFileDialog.FileName));
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

                    ResetSignal(signal);
                }
                else
                {
                    MessageBox.Show("Incorrect format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (this.currentSignal == null)
            {
                MessageBox.Show("Нет сигнала для сохранения", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!this.isSavingWindowShowing)
            {
                savingWindow = new SaveWindow(this.currentSignal, this.begin, this.end);
                savingWindow.Closed += (object sender, EventArgs e) => this.isSavingWindowShowing = false;
                savingWindow.Show();
                this.isSavingWindowShowing = true;
            }
            else
            {
                savingWindow.Topmost = true;
                savingWindow.Topmost = false;
            }
        }
    }
}