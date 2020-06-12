using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CGProject1.SignalProcessing;

namespace CGProject1.Chart {
    public class Spectrogram : FrameworkElement {
        public Spectrogram(Channel channel) {
            curChannel = channel;
        }

        public double CoeffN {
            get => coeffN; set {
                coeffN = value;
                SetupChannel(curChannel);
            }
        }

        public double BoostCoeff {
            get => boostCoeff; set {
                boostCoeff = value;
                SetupChannel(curChannel);
            }
        }

        public double SpectrogramHeight {
            get => spectrogramHeight; set {
                spectrogramHeight = value;
                SetupChannel(curChannel);
            }
        }

        public byte[][] Palette {
            get => curPalette; set {
                curPalette = value;
                InvalidateVisual();
            }
        }

        private double boostCoeff = 1.0;
        private byte[][] curPalette;
        private double spectrogramHeight = 100;
        private double coeffN = 1.0;

        private const double leftOffset = 40;
        private const double rightOffset = 60;
        private double titleOffset = 30;
        private Channel curChannel;

        private CancellationTokenSource cts;
        private CountdownEvent cde;
        private double[,] matrix;

        private double minValue;
        private double maxValue;

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);

            var title = new FormattedText(curChannel.Name,
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    14, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                );
            title.SetFontWeight(FontWeights.Bold);
            title.TextAlignment = TextAlignment.Center;

            titleOffset = title.Height + 2;
            drawingContext.DrawText(title, new Point(ActualWidth / 2 - title.Width / 2, 0));

            if (matrix == null) {
                SetupChannel(curChannel);
            }

            if (matrix != null && curPalette != null) {
                if (matrix.GetLength(1) != ActualWidth - rightOffset - leftOffset) {
                    SetupChannel(curChannel);
                }

                int width = matrix.GetLength(1);//(int)(ActualWidth - rightOffset);
                int height = matrix.GetLength(0);//(int)(ActualHeight - titleOffset);

                // step 7.1
                var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                byte[] rawImg = new byte[height * bitmap.BackBufferStride];

                for (int i = 0; i < matrix.GetLength(0); i++) {
                    for (int j = 0; j < matrix.GetLength(1); j++) {
                        byte intensity = (byte)Math.Min(255, matrix[i, j] / maxValue * 255.0 * boostCoeff);
                        for (int k = 0; k < 3; k++) {
                            rawImg[(matrix.GetLength(0) - 1 - i) * bitmap.BackBufferStride + j * 4 + k] = curPalette[intensity][2 - k];
                        }
                        rawImg[(height - 1 - i) * bitmap.BackBufferStride + j * 4 + 3] = 255;
                    }
                }

                bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                    rawImg, bitmap.PixelWidth * bitmap.Format.BitsPerPixel / 8, 0);

                drawingContext.DrawImage(bitmap, new Rect(leftOffset, titleOffset, bitmap.PixelWidth, bitmap.PixelHeight));
            }

            
        }

        private async void SetupChannel(Channel channel) {
            var task = CalculateMatrix(channel);
            var res = await task;
            if (res != null) {
                this.matrix = res;
                InvalidateVisual();
            }
        }

        private async Task<double[,]> CalculateMatrix(Channel channel) {
            if (ActualWidth == 0 || spectrogramHeight == 0) {
                return null;
            }

            int width = (int)(ActualWidth - rightOffset - leftOffset);
            int height = (int)(spectrogramHeight - titleOffset);

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
                    analyzer.SetupChannel(0, x.Length, true, true);
                    Channel amps = analyzer.AmplitudeSpectre();

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

            if (cts != null) {
                cts.Cancel();
                cde.Wait();
            }

            var countdownEvent = new CountdownEvent(threadCount);
            var token = new CancellationTokenSource();

            cde = countdownEvent;
            cts = token;

            for (int i = 0; i < threadCount; i++) {
                ThreadPool.QueueUserWorkItem(threadAction, new object[] { i, threadCount, token, countdownEvent });
            }

            await Task.Factory.StartNew(() => countdownEvent.Wait());
            if (token.IsCancellationRequested) return null;

            // step 6
            double maxVal = double.MinValue;
            double minVal = double.MaxValue;
            for (int i = 0; i < sectionsCount; i++) {
                for (int j = 0; j < samplesPerSection; j++) {
                    maxVal = Math.Max(maxVal, spectrogramMatrix[j, i]);
                    minVal = Math.Min(minVal, spectrogramMatrix[j, i]);
                }
            }

            this.maxValue = maxVal;
            this.minValue = minVal;

            this.Height = spectrogramHeight;

            return spectrogramMatrix;
        }
    }
}
