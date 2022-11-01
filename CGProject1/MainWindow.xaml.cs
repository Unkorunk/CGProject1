using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CGProject1.DtfCalculator;
using CGProject1.FileFormat;
using CGProject1.FileFormat.API;
using CGProject1.Pages;
using CGProject1.Pages.AnalyzerContainer;
using CGProject1.SignalProcessing;
using CGProject1.SignalProcessing.Models;
using Microsoft.Win32;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Layout.Serialization;


namespace CGProject1
{
    public partial class MainWindow : Window
    {
        private static readonly Settings Settings = Settings.GetInstance(nameof(MainWindow));
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly IDftCalculator DftCalculator = new FftwDftCalculator();

        public static MainWindow Instance { get; private set; }

        private ModelingWindow modelingWindow;
        private SaveWindow savingWindow;

        private bool isModelingWindowShowing = false;
        private bool isSavingWindowShowing = false;

        [NotNull] private readonly StatisticsPage statisticsPage = new StatisticsPage();

        [NotNull] private readonly LayoutAnchorable statisticsPane = new LayoutAnchorable
        {
            ContentId = "Statistics", Title = "Statistics",
            CanClose = false, CanHide = false
        };

        [NotNull] private readonly ChannelsPage channelsPage = new ChannelsPage();

        [NotNull] private readonly LayoutAnchorable channelsPane = new LayoutAnchorable
        {
            ContentId = "Channels", Title = "Channels",
            CanClose = false, CanHide = false
        };

        [NotNull] private readonly OscillogramsPage oscillogramsPage = new OscillogramsPage();

        [NotNull] private readonly LayoutAnchorable oscillogramsPane = new LayoutAnchorable
        {
            ContentId = "Oscillograms", Title = "Oscillograms",
            CanClose = false, CanHide = false
        };

        [NotNull] private readonly AnalyzerPage analyzerPage = new AnalyzerPage(DftCalculator);

        [NotNull] private readonly LayoutAnchorable analyzerPane = new LayoutAnchorable()
        {
            ContentId = "Analyzer", Title = "Fourier analysis",
            CanClose = false, CanHide = false
        };

        [NotNull] private readonly SpectrogramsPage spectrogramsPage = new SpectrogramsPage(DftCalculator);

        [NotNull] private readonly LayoutAnchorable spectrogramsPane = new LayoutAnchorable
        {
            ContentId = "Spectrograms", Title = "Spectrograms",
            CanClose = false, CanHide = false
        };

        [NotNull] private readonly AboutSignalPage aboutSignalPage = new AboutSignalPage();

        [NotNull] private readonly LayoutAnchorable aboutSignalPane = new LayoutAnchorable
        {
            ContentId = "AboutSignal", Title = "About signal",
            CanClose = false, CanHide = false
        };

        [NotNull] private readonly MicrophonePage microphonePage = new MicrophonePage();

        [NotNull] private readonly LayoutAnchorable microphonePane = new LayoutAnchorable
        {
            ContentId = "Microphone", Title = "Microphone",
            CanClose = false, CanHide = false
        };

        [NotNull] private readonly IChannelComponent[] pages;
        [NotNull] private readonly LayoutAnchorable[] panes;

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
                        { Modelling.discreteModels, Modelling.continiousModels, Modelling.randomModels });
                Settings.Save();
            };

            LeftPane.Children.Add(channelsPane);
            UpperMiddlePane.Children.Add(oscillogramsPane);
            RightPane.Children.Add(aboutSignalPane);
            RightPane.Children.Add(statisticsPane);
            RightPane.Children.Add(microphonePane);
            LowerMiddlePane.Children.Add(analyzerPane);
            LowerMiddlePane.Children.Add(spectrogramsPane);

            pages = new IChannelComponent[]
            {
                channelsPage, aboutSignalPage, statisticsPage,
                oscillogramsPage, analyzerPage, spectrogramsPage,
                microphonePage
            };

            panes = new LayoutAnchorable[]
            {
                channelsPane, aboutSignalPane, statisticsPane,
                oscillogramsPane, analyzerPane, spectrogramsPane,
                microphonePane
            };

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

            Task.WaitAll(ResetSignal(null));
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

        public async Task UpdateActiveSegment(int begin, int end)
        {
            this.begin = begin;
            this.end = end;

            var tasks = pages.Select(page => page.UpdateActiveSegment(begin, end));
            await Task.WhenAll(tasks);
        }

        public async Task ResetSignal(Signal newSignal)
        {
            CloseAll();

            foreach (var page in pages)
            {
                page.Reset(newSignal);
            }

            Modelling.ResetCounters();

            currentSignal = newSignal;
            oscillogramsPage.Reset(newSignal);

            if (currentSignal == null)
            {
                Logger.Info("Signal was reset to null");
                return;
            }

            await UpdateActiveSegment(0, newSignal.SamplesCount - 1);

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

        private async void OpenFileClick(object sender, RoutedEventArgs e)
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

                await using var fileStream = new FileStream(openFileDialog.FileName, FileMode.Open);
                if (reader.TryRead(fileStream, out var fileInfo))
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

                    await ResetSignal(signal);
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

        private void LoadLayout()
        {
            if (!File.Exists("lastLayout"))
            {
                return;
            }

            var xmlDeserializer = new XmlLayoutSerializer(MyDockingManager);
            using (var reader = new StreamReader("lastLayout"))
            {
                xmlDeserializer.LayoutSerializationCallback += (s, e) =>
                {
                    object o = e.Content;
                };
                xmlDeserializer.Deserialize(reader);
            }
        }
    }
}