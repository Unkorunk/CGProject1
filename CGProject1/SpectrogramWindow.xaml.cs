using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CGProject1.SignalProcessing;

namespace CGProject1 {
    public partial class SpectrogramWindow : Window {
        public SpectrogramWindow() {
            InitializeComponent();

            channels = new List<Channel>();
        }

        static SpectrogramWindow() {
            greyPalette = new byte[256][];
            
            for (int i = 0; i < 256; i++) {
                greyPalette[i] = new byte[3];
                for (int j = 0; j < 3; j++) {
                    greyPalette[i][j] = (byte)i;
                }
            }
        }

        private static byte[][] greyPalette;



        private List<Channel> channels;

        private byte[][] curPalette = greyPalette;

        public void AddChannel(Channel channel) {
            int width = (int)Spectrograms.RenderSize.Width - 20;
            int height = 100;

            double coeffN = 10;

            int sectionsCount = width;
            int samplesPerSection = height;

            var spectrogramMatrix = new double[samplesPerSection, sectionsCount];

            double sectionBase = (double) channel.SamplesCount / sectionsCount;

            int sectionN = (int)(sectionBase * coeffN);

            int l = (int)Math.Max(1, samplesPerSection * 4.0 / sectionN);
            int nn = sectionN * l;

            double[] x = new double[nn];

            double maxVal = double.MinValue;

            for (int i = 0; i < sectionsCount; i++) {
                int start = i * (int)sectionBase;
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

                // Avrg
                for (int j = 0; j < sectionN; j++) {
                    x[j] -= avrg;
                }

                // Hamming window
                for (int j = 0; j < sectionN; j++) {
                    double w = 0.54 - 0.46 * Math.Cos(2 * Math.PI * j / (Math.Max(1, sectionN - 1)));
                    x[j] *= w;
                }

                for (int j = sectionN; j < nn; j++) {
                    x[j] = 0;
                }

                var analyzer = new Analyzer(x, channel.SamplingFrq);
                analyzer.SetupChannel(0, nn - 1, true, true);
                Channel amps = analyzer.AmplitudeSpectre();

                // Sliding window
                var q = new LinkedList<double>();
                double curWindow = 0;

                for (int j = -(l - 1) / 2; j <= l / 2; j++) {
                    curWindow += amps.values[Math.Abs(0 + j) % amps.values.Length];
                    q.AddLast(amps.values[Math.Abs(0 + j) % amps.values.Length]);
                }

                amps.values[0] = curWindow / l;

                for (int j = 0; j < amps.values.Length; j++) {
                    curWindow -= q.First.Value;
                    q.RemoveFirst();
                    double curVal = amps.values[(j + l / 2) % amps.values.Length];
                    curWindow += curVal;
                    q.AddLast(curVal);

                    amps.values[j] = curWindow / l;
                }

                for (int j = 0; j < samplesPerSection; j++) {
                    spectrogramMatrix[j, i] = amps.values[j];
                    maxVal = Math.Max(maxVal, spectrogramMatrix[j, i]);
                }
            }

            double boostCoef = 100;
            byte[] rawImg = new byte[samplesPerSection * sectionsCount * 3];

            for (int i = 0; i < samplesPerSection; i++) {
                for (int j = 0; j < sectionsCount; j++) {
                    byte intensity = Math.Min((byte)255, (byte)(spectrogramMatrix[i, j] / maxVal * boostCoef));
                    for (int k = 0; k < 3; k++) {
                        rawImg[i * sectionsCount + j + k] = curPalette[intensity][2 - k];
                    }
                }
            }

            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
            var rect = new Int32Rect(0, 0, width, height);
            wb.WritePixels(rect, rawImg, width * 3, 0);

            var pic = new Image();
            pic.Source = wb;

            Spectrograms.Children.Add(pic);
        }
    }
}
