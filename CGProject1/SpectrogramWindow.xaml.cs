using System;
using System.Windows;
using System.Threading;
using System.Windows.Media;
using System.Windows.Controls;
using CGProject1.SignalProcessing;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using CGProject1.Chart;

namespace CGProject1 {
    public partial class SpectrogramWindow : Window {
        public SpectrogramWindow() {
            channelNames = new HashSet<string>();
            pics = new List<Image>();
            channels = new List<Channel>();
            InitializeComponent();
            
        }

        static SpectrogramWindow()
        {
            greyPalette = new byte[256][];
            hotPalette = new byte[256][];

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
        }

        private static byte[][] hotPalette;
        private static byte[][] greyPalette;

        private byte[][] curPalette = hotPalette;

        private HashSet<String> channelNames;
        private List<Image> pics;
        private List<Channel> channels;

        public void AddChannel(Channel channel) {
            if (channelNames.Contains(channel.Name)) {
                return;
            }

            channelNames.Add(channel.Name);
            channels.Add(channel);

            var border = new Border();
            border.BorderThickness = new Thickness(1);
            border.BorderBrush = Brushes.Black;

            var channelPanel = new StackPanel();
            border.Child = channelPanel;

            var label = new Label();
            label.Content = $"Канал: {channel.Name}";
            label.HorizontalContentAlignment = HorizontalAlignment.Center;

            channelPanel.Children.Add(label);

            var bitmap = CalculateBitmap(channel);

            // step 7.2
            Image pic = new Image();
            pic.Source = bitmap;
            pic.Stretch = Stretch.Fill;
            pic.Height = 100;

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

        private WriteableBitmap CalculateBitmap(Channel channel) {
            int width = (int)Spectrograms.RenderSize.Width;
            int height = 100;

            const double coeffN = 1;
            const double boostCoef = 255;

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
            ThreadPool.GetMaxThreads(out var threadCount, out var completionPortThreads);
            CountdownEvent countdownEvent = new CountdownEvent(threadCount);
            WaitCallback threadAction = (obj) => {
                int offset = ((int[])obj)[0];
                int step = ((int[])obj)[1];
                for (int i = offset; i < sectionsCount; i += step) {
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
                    analyzer.SetupChannel(0, x.Length - 1, false, true);
                    Channel amps = analyzer.AmplitudeSpectre();
                    if (amps.values.Length != NN / 2) {
                        throw new Exception();
                    }

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

                countdownEvent.Signal();
            };

            for (int i = 0; i < threadCount; i++) {
                ThreadPool.QueueUserWorkItem(threadAction, new int[] { i, threadCount });
            }

            countdownEvent.Wait();

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
                    byte intensity = Math.Min((byte)255, (byte)(spectrogramMatrix[i, j] / maxVal * boostCoef));
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

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
            //for (int i = 0; i < channels.Count; i++) {
            //    //pics[i].Source = CalculateBitmap(channels[i]);
            //    pics[i].Width = Spectrograms.RenderSize.Width;
            //}
        }
    }
}
