using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CGProject1.SignalProcessing;

namespace CGProject1.Chart {
    public class Spectrogram : FrameworkElement {
        public Spectrogram(Channel channel) {
            curChannel = channel;
            this.begin = 0;
            this.end = channel.SamplesCount - 1;

            SizeChanged += (object sender, SizeChangedEventArgs e) => {
                SetupChannel(curChannel);
            };
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
                InvalidateVisual();
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

        public double SpectrogramWidth {
            get => matrix == null ? 0 : matrix.GetLength(1); 
        }

        public double LeftOffset {
            get => leftOffset;
        }

        public double RightOffset {
            get => rightOffset;
        }

        public int Begin {
            get => begin; set {
                begin = value;
                SetupChannel(curChannel);
            }
        }

        public int End {
            get => end; set {
                end = value;
                SetupChannel(curChannel);
            } 
        }

        public bool ShowCurrentXY { get; set; }

        private int begin;
        private int end;

        private double boostCoeff = 1.0;
        private byte[][] curPalette;
        private double spectrogramHeight = 150;
        private double coeffN = 1.0;

        private const double leftOffset = 45;
        private const double rightOffset = 95;
        private const double paletteOffset = 2.5;
        private const double paletteWidth = 30;

        private double titleOffset = 30;
        private Channel curChannel;

        private CancellationTokenSource cts;
        private CountdownEvent cde;
        private double[,] matrix;

        private double minValue;
        private double maxValue;

        private double stepX = 1;
        private double stepY = 1;

        private int curSelectedX = -1;
        private int curSelectedY = -1;

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            int lenX = 0;
            int lenY = 0;
            double matrixVal = 0;

            if (matrix != null) {
                var curMatrix = this.matrix;

                lenX = curMatrix.GetLength(1);
                lenY = curMatrix.GetLength(0);

                stepX = (ActualWidth - rightOffset - leftOffset) / (lenX);
                stepY = (ActualHeight - titleOffset) / (lenY);

                if (curSelectedX != -1 && curSelectedY != -1 && curSelectedY < curMatrix.GetLength(0) && curSelectedX < curMatrix.GetLength(1)) {
                    matrixVal = curMatrix[lenY - curSelectedY - 1, curSelectedX] * boostCoeff;
                }

            }

            double curLen = this.End - this.Begin + 1;

            DrawTitle(drawingContext);

            if (matrix == null) {
                SetupChannel(curChannel);
            }

            if (matrix != null && curPalette != null) {
                DrawBitmap(drawingContext);
                DrawBrightness(drawingContext);
            }

            if (lenX != 0 && lenY != 0 && curSelectedX != -1 && curSelectedY != -1) {
                double centerX = 0.0;

                centerX = leftOffset + stepX * curSelectedX;

                drawingContext.DrawLine(new Pen(Brushes.Green, 2.0),
                    new Point(centerX, titleOffset),
                    new Point(centerX, ActualHeight));

                var formText1 = new FormattedText($"X: { curChannel.StartDateTime + TimeSpan.FromSeconds((this.begin + curSelectedX * curLen / lenX) * curChannel.DeltaTime):dd-MM-yyyy \n HH\\:mm\\:ss}",
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    12, Brushes.DarkGreen, VisualTreeHelper.GetDpi(this).PixelsPerDip
                );

                formText1.TextAlignment = TextAlignment.Center;

                drawingContext.DrawRectangle(Brushes.LightGray, new Pen(Brushes.Black, 2.0), new Rect(
                    centerX - formText1.Width / 2, 0, formText1.Width, formText1.Height
                ));
                drawingContext.DrawText(formText1, new Point(centerX, 0.0));

                double centerY = titleOffset + stepY * curSelectedY;

                drawingContext.DrawLine(new Pen(Brushes.Green, 2.0),
                    new Point(leftOffset, centerY),
                    new Point(ActualWidth - rightOffset, centerY)
                );

                //string val = Math.Round(this.Channel.values[curSelected], 5).ToString(CultureInfo.InvariantCulture);
                //if (val.Length > this.MaxVAxisLength) {
                //    val = val.Substring(0, this.MaxVAxisLength);
                //}
                double yValStep = curChannel.SamplingFrq / 2 / lenY;
                string valStr = "F:" + ((lenY - curSelectedY) * yValStep).ToString(CultureInfo.InvariantCulture);
                if (valStr.Length > 8) {
                    valStr = valStr.Substring(0, 8);
                }

                string valStr2 = "T:"+(1.0 / ((lenY - curSelectedY) * yValStep)).ToString(CultureInfo.InvariantCulture);
                if (valStr2.Length > 8)
                {
                    valStr2 = valStr2.Substring(0, 8);
                }

                var formText2 = new FormattedText($"{valStr}\n{valStr2}",
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    12, Brushes.DarkGreen, VisualTreeHelper.GetDpi(this).PixelsPerDip
                );
                formText2.TextAlignment = TextAlignment.Right;

                drawingContext.DrawRectangle(Brushes.LightGray, new Pen(Brushes.Black, 2.0), new Rect(
                    leftOffset - formText2.Width, centerY - formText2.Height / 2, formText2.Width, formText2.Height
                ));
                drawingContext.DrawText(formText2, new Point(leftOffset, centerY - formText2.Height / 2));

                var matrixStrVal = matrixVal.ToString(CultureInfo.InvariantCulture);
                if (matrixStrVal.Length > 8) {
                    matrixStrVal = matrixStrVal.Substring(0, 8);
                }

                var formText3 = new FormattedText($"I: {matrixStrVal}",
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    14, Brushes.DarkGreen, VisualTreeHelper.GetDpi(this).PixelsPerDip
                );
                formText3.SetFontWeight(FontWeights.Bold);
                formText3.TextAlignment = TextAlignment.Right;

                double posX = centerX - formText3.Width / 2;
                if (posX < formText3.Width) {
                    posX = centerX + formText3.Width;
                }

                drawingContext.DrawRectangle(Brushes.LightGray, new Pen(Brushes.Black, 2.0), new Rect(
                   posX - formText3.Width, centerY - formText3.Height / 2, formText3.Width, formText3.Height
               ));
                drawingContext.DrawText(formText3, new Point(posX, centerY - formText3.Height / 2));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);

            var position = e.GetPosition(this);
            if (position.X >= leftOffset && position.X <= ActualWidth -  rightOffset
                && position.Y >= titleOffset) {
                if (ShowCurrentXY) {
                    curSelectedX = GetXIdx(position);
                    curSelectedY = GetYIdx(position);
                }
            } else if (curSelectedX != -1) {
                curSelectedX = -1;
                curSelectedY = -1;
            }
            InvalidateVisual();
        }

        protected override void OnMouseLeave(MouseEventArgs e) {
            base.OnMouseLeave(e);
            if (ShowCurrentXY) {
                if (curSelectedX != -1) {
                    curSelectedX = -1;
                    curSelectedY = -1;
                    InvalidateVisual();
                }
            }
        }

        private int GetXIdx(Point position) {
            position.X -= leftOffset;

            int idx;

            idx = (int)Math.Round(position.X / stepX);
            
            return idx;
        }

        private int GetYIdx(Point position) {
            position.Y -= titleOffset;

            int idx;

            idx = (int)Math.Round(position.Y / stepY);

            return idx;
        }

        private void DrawTitle(DrawingContext drawingContext) {
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
        }

        private void DrawBitmap(DrawingContext drawingContext) {
            //if (Math.Abs(matrix.GetLength(1) - (ActualWidth - rightOffset - leftOffset)) > 1e-5) {
            //    SetupChannel(curChannel);
            //}

            int width = matrix.GetLength(1);
            int height = matrix.GetLength(0);

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

            drawingContext.DrawImage(bitmap, new Rect(leftOffset, titleOffset, Math.Max(0, ActualWidth - rightOffset - leftOffset), bitmap.PixelHeight));

            double minFrq = 0;
            double maxFrq = curChannel.SamplingFrq / 2;

            for (int i = 0; i < 5; i++) {
                double y = i * height / 4;

                drawingContext.DrawLine(new Pen(Brushes.Gray, 1.0), new Point(leftOffset - 4, titleOffset + y),
                       new Point(leftOffset, titleOffset + y));

                string val = Math.Round(maxFrq - (i) * (maxFrq - minFrq) / 4, 5).ToString(CultureInfo.InvariantCulture);
                if (val.Length > 8) {
                    val = val.Substring(0, 8);
                }

                if (val.Length > 12) {
                    val = val.Substring(0, 12);
                }

                var formText1 = new FormattedText(val,
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                );

                if (i == 0) {
                    y += formText1.Height / 2;
                } else if (i == 4) {
                    y -= formText1.Height / 2;
                }

                formText1.TextAlignment = TextAlignment.Right;

                drawingContext.DrawText(formText1, new Point(leftOffset - 5, titleOffset + y - formText1.Height / 2));
            }

            drawingContext.DrawRectangle(Brushes.Transparent, new Pen(Brushes.Black, 1.0), new Rect(leftOffset, titleOffset, Math.Max(0, ActualWidth - rightOffset - leftOffset), bitmap.PixelHeight));
        }

        private void DrawBrightness(DrawingContext drawingContext) {
            var bitmap = new WriteableBitmap((int)paletteWidth, 256, 96, 96, PixelFormats.Bgra32, null);
            var rawImg = new byte[256 * bitmap.BackBufferStride];

            int height = matrix.GetLength(0);

            for (int i = 0; i < 256; i++) {
                for (int j = 0; j < (int)paletteWidth; j++) {
                    for (int k = 0; k < 3; k++) {
                        rawImg[(255 - i) * bitmap.BackBufferStride + j * 4 + k] = curPalette[(byte)Math.Min(255, i * boostCoeff)][2 - k];
                    }
                    rawImg[(255 - i) * bitmap.BackBufferStride + j * 4 + 3] = 255;
                }
            }

            bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
               rawImg, bitmap.PixelWidth * bitmap.Format.BitsPerPixel / 8, 0);

            drawingContext.DrawImage(bitmap, new Rect(ActualWidth - rightOffset + 5, titleOffset, bitmap.PixelWidth, height));

            for (int i = 0; i < 5; i++) {
                double y = i * height / 4;

                drawingContext.DrawLine(new Pen(Brushes.Gray, 1.0), new Point(ActualWidth - rightOffset + 5 + bitmap.PixelWidth, titleOffset + y),
                       new Point(ActualWidth - rightOffset + 5 + bitmap.PixelWidth + 4, titleOffset + y));

                string val = Math.Round(maxValue - (i) * (maxValue - minValue) / 4, 5).ToString(CultureInfo.InvariantCulture);
                if (val.Length > 8) {
                    val = val.Substring(0, 8);
                }

                if (val.Length > 12) {
                    val = val.Substring(0, 12);
                }

                var formText1 = new FormattedText(val,
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                );

                if (i == 0) {
                    y += formText1.Height / 2;
                } else if (i == 4) {
                    y -= formText1.Height / 2;
                }

                formText1.TextAlignment = TextAlignment.Left;

                drawingContext.DrawText(formText1, new Point(ActualWidth - rightOffset + 5 + bitmap.PixelWidth + 6, titleOffset + y - formText1.Height / 2));
            }

            drawingContext.DrawRectangle(Brushes.Transparent, new Pen(Brushes.Black, 1.0), new Rect(ActualWidth - rightOffset + 5, titleOffset, bitmap.PixelWidth, height));
        }

        private async void SetupChannel(Channel channel) {
            var task = CalculateMatrix(channel, this.begin, this.end);
            var res = await task;
            if (res != null) {
                this.matrix = res;
                InvalidateVisual();
            }
        }

        private async Task<double[,]> CalculateMatrix(Channel channel, int left, int right) {
            if (ActualWidth == 0 || spectrogramHeight == 0) {
                return null;
            }

            int len = right - left + 1;

            int width = (int)(ActualWidth - rightOffset - leftOffset);
            int height = (int)(spectrogramHeight - titleOffset);

            // step 1
            int sectionsCount = width;
            int samplesPerSection = height;

            if (width <= 0 || height <= 0 ) {
                return null;
            }

            var spectrogramMatrix = new double[samplesPerSection, sectionsCount];

            // step 2
            double sectionBase = (double)len / sectionsCount;

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
                        if (j + start >= len) {
                            x[j] = 0;
                        } else {
                            x[j] = channel.values[left + j + start];
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
