using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CGProject1.Pages;
using CGProject1.Pages.AnalyzerContainer;
using CGProject1.SignalProcessing;
using Microsoft.Win32;
using FileFormats;

using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Layout.Serialization;


namespace CGProject1
{
    public partial class MainWindow : Window
    {
        private static readonly Settings Settings = Settings.GetInstance(nameof(MainWindow));
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        
        public static MainWindow Instance { get; private set; }

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
            Instance = this;
            InitializeComponent();

            Closed += (sender, e) =>
            {
                CloseAll();

                Serializer.SerializeModels(Modelling.defaultPath,
                    new List<ChannelConstructor>[]
                        {Modelling.discreteModels, Modelling.continiousModels, Modelling.randomModels});
                Settings.Save();
            };

            statisticsPage = new StatisticsPage();
            statisticsPane = new LayoutAnchorable {ContentId = "Statistics", Title = "Статистики", CanClose = false, CanHide = false };
            RightPane.Children.Add(statisticsPane);

            channelsPage = new ChannelsPage();
            channelsPane = new LayoutAnchorable {ContentId = "Channels", Title = "Каналы", CanClose = false, CanHide = false};
            LeftPane.Children.Add(channelsPane);

            oscillogramsPage = new OscillogramsPage();
            oscillogramsPane = new LayoutAnchorable {ContentId = "Oscillograms", Title = "Осциллограммы", CanClose = false, CanHide = false };
            UpperMiddlePane.Children.Add(oscillogramsPane);

            analyzerPage = new AnalyzerPage();
            analyzerPane = new LayoutAnchorable {ContentId = "Analyzer", Title = "Анализ Фурье", CanClose = false, CanHide = false };
            LowerMiddlePane.Children.Add(analyzerPane);

            spectrogramsPage = new SpectrogramsPage();
            spectrogramsPane = new LayoutAnchorable {ContentId = "Spectrograms", Title = "Спектрограммы", CanClose = false, CanHide = false };
            LowerMiddlePane.Children.Add(spectrogramsPane);

            aboutSignalPage = new AboutSignalPage();
            aboutSignalPane = new LayoutAnchorable {ContentId = "AboutSignal", Title = "О сигнале", CanClose = false, CanHide = false };
            RightPane.Children.Add(aboutSignalPane);

            pages = new IChannelComponent[]
                {channelsPage, aboutSignalPage, statisticsPage, oscillogramsPage, analyzerPage, spectrogramsPage};
            panes = new LayoutAnchorable[]
                {channelsPane, aboutSignalPane, statisticsPane, oscillogramsPane, analyzerPane, spectrogramsPane};

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
            LoadLayout();
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
                Logger.Info("Signal was reset to null");
                return;
            }

            UpdateActiveSegment(0, newSignal.SamplesCount - 1);

            for (int i = 0; i < currentSignal.channels.Count; i++)
            {
                channelsPage.AddChannel(currentSignal.channels[i]);
            }

            Logger.Info("Signal was reset successfully");
            Logger.Info($"Current signal {newSignal}");
        }

        private void OpenPane(LayoutAnchorable pane)
        {
            Logger.Info($"Layout {pane.Title} opened");
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
            Logger.Info("Modeling window opened");
        }

        public void AddChannel(Channel channel)
        {
            channel.StartDateTime = this.currentSignal.StartDateTime;
            channel.SamplingFrq = this.currentSignal.SamplingFrq;

            this.currentSignal.channels.Add(channel);

            modelingWindow?.Close();

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
                Filter = "all files (*.txt;*.wav;*.wave;*.dat;*.mp3)|*.txt;*.wav;*.wave;*.dat;*.mp3|" +
                         "txt files (*.txt)|*.txt|" +
                         "wave files (*.wav;*.wave)|*.wav;*.wave|" +
                         "dat files (*.dat)|*.dat|" +
                         "mp3 files (*.mp3)|*.mp3"
                         
            };

            openFileDialog.FilterIndex = Settings.GetOrDefault("filterIndex", openFileDialog.FilterIndex);

            if (openFileDialog.ShowDialog() == true)
            {
                IReader reader;
                switch (Path.GetExtension(openFileDialog.FileName))
                {
                    case ".mp3":
                        reader = new Mp3Reader();
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

                    Logger.Info($"File {openFileDialog.FileName} was opened");

                    signal.UpdateChannelsInfo();

                    ResetSignal(signal);
                }
                else
                {
                    MessageBox.Show("Incorrect format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Logger.Info($"File {openFileDialog.FileName} has incorrect format");
                }
                
                Settings.Set("filterIndex", openFileDialog.FilterIndex);
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
            Logger.Info("Save window opened");
        }

        private void MainWindow_OnClosed(object? sender, EventArgs e)
        {
            var xmlSerializer = new XmlLayoutSerializer(MyDockingManager);
            using (var writer = new StreamWriter("lastLayout"))
            {
                xmlSerializer.Serialize(writer);
            }
        }

        private void LoadLayout() {
            if (!File.Exists("lastLayout")) {
                return;
            }

            var xmlDeserializer = new XmlLayoutSerializer(MyDockingManager);
            using (var reader = new StreamReader("lastLayout")) {
                xmlDeserializer.LayoutSerializationCallback += (s, e) =>
                {
                    object o = e.Content;
                };
                xmlDeserializer.Deserialize(reader);
            }
        }
    }
}