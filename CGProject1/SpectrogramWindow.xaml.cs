using System;
using System.Windows;
using System.Threading;
using System.Windows.Media;
using System.Windows.Controls;
using CGProject1.SignalProcessing;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using CGProject1.Chart;
using System.Windows.Input;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;

namespace CGProject1 {
    public partial class SpectrogramWindow : Window {
        private static List<byte[][]> palettes;

        private byte[][] curPalette = palettes[1];

        private double spectrogramHeight = 100;
        private double coeffN = 1.0;
        private double boostCoeff = 1.0;

        private HashSet<string> channelNames;
        private List<Image> pics;
        private List<Channel> channels;
        private Dictionary<string, CancellationTokenSource> cts;
        private Dictionary<string, CountdownEvent> cde;

        public SpectrogramWindow() {
            channelNames = new HashSet<string>();
            pics = new List<Image>();
            channels = new List<Channel>();

            cts = new Dictionary<string, CancellationTokenSource>();
            cde = new Dictionary<string, CountdownEvent>();

            InitializeComponent();
        }

        static SpectrogramWindow()
        {
            palettes = new List<byte[][]>();

            var greyPalette = new byte[256][];
            var hotPalette = new byte[256][];

            for (int i = 0; i <= 85; i++) hotPalette[i] = new byte[3] { (byte)(i * 3), 0, 0 };
            for (int i = 86; i <= 170; i++) hotPalette[i] = new byte[3] { 255, (byte)((i - 85) * 3), 0 };
            for (int i = 171; i <= 255; i++) hotPalette[i] = new byte[3] { 255, 255, (byte)((i - 170) * 3) };

            for (int i = 0; i < 256; i++)
            {
                greyPalette[i] = new byte[3];
                for (int j = 0; j < 3; j++)
                {
                    greyPalette[i][j] = (byte)i;
                }
            }

            palettes.Add(greyPalette);
            palettes.Add(hotPalette);
        }

        public async void AddChannel(Channel channel) {
            if (channelNames.Contains(channel.Name)) {
                return;
            }

            cts.Add(channel.Name, null);
            cde.Add(channel.Name, null);

            channelNames.Add(channel.Name);
            channels.Add(channel);

            var border = new Border();
            border.BorderThickness = new Thickness(1);
            border.BorderBrush = Brushes.Black;

            var channelPanel = new StackPanel();
            border.Child = channelPanel;

            var label = new Label();
            label.Content = $"Канал: {channel.Name}";
            label.FontWeight = FontWeights.Bold;
            label.HorizontalContentAlignment = HorizontalAlignment.Center;

            channelPanel.Children.Add(label);

            var task = CalculateBitmap(channel);
            var bitmap = await task;

            // step 7.2
            Image pic = new Image();
            pic.Source = bitmap;
            pic.Stretch = Stretch.Fill;
            pic.Height = spectrogramHeight;

            channelPanel.Children.Add(pic);
            pics.Add(pic);

            var chart = new ChartLine(in channel);
            chart.Height = 50;
            chart.Begin = 0;
            chart.End = channel.SamplesCount;
            chart.DisplayTitle = false;

            channelPanel.Children.Add(chart);

            Spectrograms.Children.Add(border);
        }

        private async Task<WriteableBitmap> CalculateBitmap(Channel channel) {
            int width = (int)Spectrograms.RenderSize.Width;
            int height = (int)spectrogramHeight;

            // step 1
            int sectionsCount = width;
            int samplesPerSection = height;

            var spectrogramMatrix = new double[samplesPerSection, sectionsCount];

            // step 2
            double sectionBase = (double)channel.SamplesCount / sectionsCount;

            // step 3
            int sectionN = (int)(sectionBase * coeffN);

            // step 4
            int N = 2 * samplesPerSection;

            int NN = N;
            if (sectionN > N) {
                int mult = (sectionN + N - 1) / N;
                NN = mult * N;
            }
            int l = NN / N;

            // step 5
            WaitCallback threadAction = (obj) => {
                var offset = (int)((object[])obj)[0];
                var step = (int)((object[])obj)[1];
                var cts = (CancellationTokenSource)((object[])obj)[2];
                var cde = (CountdownEvent)((object[])obj)[3];

                for (int i = offset; i < sectionsCount && !cts.IsCancellationRequested; i += step) {
                    double[] x = new double[NN];

                    // step 5.1
                    int start = (int)(i * sectionBase);

                    // step 5.3
                    double avrg = 0;
                    for (int j = 0; j < sectionN; j++) {
                        if (j + start >= channel.values.Length) {
                            x[j] = 0;
                        } else {
                            x[j] = channel.values[j + start];
                        }

                        avrg += x[j];
                    }

                    avrg /= sectionN;


                    for (int j = 0; j < sectionN; j++) {
                        x[j] -= avrg;
                    }

                    // step 5.4
                    for (int j = 0; j < sectionN; j++) {
                        double w = 0.54 - 0.46 * Math.Cos(2 * Math.PI * j / Math.Max(1, sectionN - 1));
                        x[j] *= w;
                    }

                    // step 5.5
                    for (int j = sectionN; j < NN; j++) {
                        x[j] = 0;
                    }

                    // step 5.6
                    var analyzer = new Analyzer(x, channel.SamplingFrq);
                    analyzer.SetupChannel(0, x.Length * 2, true, true);
                    Channel amps = analyzer.AmplitudeSpectre();
                    //if (amps.values.Length != NN / 2) {
                    //    throw new Exception();
                    //}

                    int L1 = -(l - 1) / 2, L2 = l / 2;
                    for (int k = 0; k < samplesPerSection; k++) {
                        double sumAvg = 0.0;
                        for (int j = L1; j <= L2; j++) {
                            sumAvg += amps.values[Math.Abs(l * k + j) % amps.values.Length];
                        }
                        amps.values[k] = sumAvg / l;
                    }

                    // step 5.7
                    for (int j = 0; j < samplesPerSection; j++) {
                        spectrogramMatrix[j, i] = amps.values[j];
                    }
                }

                cde.Signal();
            };

            ThreadPool.GetMaxThreads(out var threadCount, out var completionPortThreads);
            
            if (cts[channel.Name] != null) {
                cts[channel.Name].Cancel();
                cde[channel.Name].Wait();
            }

            var countdownEvent = new CountdownEvent(threadCount);
            var token = new CancellationTokenSource();

            cde[channel.Name] = countdownEvent;
            cts[channel.Name] = token;

            for (int i = 0; i < threadCount; i++) {
                ThreadPool.QueueUserWorkItem(threadAction, new object[] { i, threadCount, token, countdownEvent });
            }

            await Task.Factory.StartNew(() => countdownEvent.Wait());
            if (token.IsCancellationRequested) return null;

            // step 6
            double maxVal = double.MinValue;
            for (int i = 0; i < sectionsCount; i++) {
                for (int j = 0; j < samplesPerSection; j++) {
                    maxVal = Math.Max(maxVal, spectrogramMatrix[j, i]);
                }
            }

            // step 7.1
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            byte[] rawImg = new byte[height * bitmap.BackBufferStride];

            for (int i = 0; i < height; i++) {
                for (int j = 0; j < width; j++) {
                    byte intensity = Math.Min((byte)255, (byte)(spectrogramMatrix[i, j] / maxVal * 256.0 * boostCoeff));
                    for (int k = 0; k < 3; k++) {
                        rawImg[(height - 1 - i) * bitmap.BackBufferStride + j * 4 + k] = curPalette[intensity][2 - k];
                    }
                    rawImg[(height - 1 - i) * bitmap.BackBufferStride + j * 4 + 3] = 255;
                }
            }

            bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                rawImg, bitmap.PixelWidth * bitmap.Format.BitsPerPixel / 8, 0);

            return bitmap;
        }

        private async void RedrawSpectrograms() {
            for (int i = 0; i < channels.Count; i++) {
                var task = CalculateBitmap(channels[i]);
                var source = await task;
                if (source != null)
                {
                    pics[i].Source = source;
                    pics[i].Height = this.spectrogramHeight;
                }
            }
        }

        private void UpdateSpectrograms(object sender, RoutedEventArgs e) {
            if (!double.TryParse(BrightnessField.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double newBrightness)
                    || !double.TryParse(HeightField.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double newHeight)
                    || !double.TryParse(CoeffSelector.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double newCoeff))
            {
                MessageBox.Show("Некорректные параметры", "Error", MessageBoxButton.OK);
                return;
            }

            if (newCoeff < 1) {
                newCoeff = 1;
            }
            if (newCoeff > 10) {
                newCoeff = 10;
            }

            BrightnessField.Text = newBrightness.ToString(CultureInfo.InvariantCulture);
            HeightField.Text = newHeight.ToString(CultureInfo.InvariantCulture);
            CoeffSelector.Text = newCoeff.ToString(CultureInfo.InvariantCulture);

            this.boostCoeff = newBrightness;
            this.spectrogramHeight = newHeight;
            this.coeffN = newCoeff;

            this.CoeffSlider.Value = newCoeff;

            RedrawSpectrograms();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
            RedrawSpectrograms();
        }

        private void ComboBoxMode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (PaletteSelector.SelectedIndex >= 0 && PaletteSelector.SelectedIndex < palettes.Count) {
                curPalette = palettes[PaletteSelector.SelectedIndex];
            }

            RedrawSpectrograms();
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
            return input.All(c => char.IsDigit(c) || char.IsControl(c) || c == '.');
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            this.coeffN = e.NewValue;
            CoeffSelector.Text = this.coeffN.ToString(CultureInfo.InvariantCulture);
            RedrawSpectrograms();
        }
    }
}
