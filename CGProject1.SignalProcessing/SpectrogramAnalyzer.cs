namespace CGProject1.SignalProcessing
{
    public class SpectrogramAnalyzer
    {
        public class CalculationResult
        {
            public readonly double[,] matrix;
            public readonly double minValue;
            public readonly double maxValue;

            public CalculationResult(double[,] matrix, double minValue, double maxValue)
            {
                this.matrix = matrix;
                this.minValue = minValue;
                this.maxValue = maxValue;
            }
        }

        private readonly Channel myChannel;
        private readonly IDftCalculator myDftCalculator;

        public SpectrogramAnalyzer(Channel channel, IDftCalculator dtfCalculator)
        {
            myChannel = channel;
            myDftCalculator = dtfCalculator;
        }

        public CalculationResult? CalculateMatrix(int left, int right, int width, int height, double coeffN,
            CancellationToken token)
        {
            int len = right - left + 1;


            // step 1
            int sectionsCount = width;
            int samplesPerSection = height;

            if (width <= 0 || height <= 0)
            {
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
            if (sectionN > N)
            {
                int mult = (sectionN + N - 1) / N;
                NN = mult * N;
            }

            int l = NN / N;

            ThreadPool.GetMaxThreads(out var threadCount, out var completionPortThreads);
            var countdownEvent = new CountdownEvent(threadCount);

            // step 5
            WaitCallback threadAction = (obj) =>
            {
                var offset = (int)obj;

                for (var i = offset; i < sectionsCount; i += threadCount)
                {
                    if (token.IsCancellationRequested) break;

                    var x = new double[NN];

                    // step 5.1
                    var start = (int)(i * sectionBase);

                    // step 5.3
                    double avrg = 0;
                    for (var j = 0; j < sectionN; j++)
                    {
                        if (j + start >= len)
                        {
                            x[j] = 0;
                        }
                        else
                        {
                            x[j] = myChannel.values[left + j + start];
                        }

                        avrg += x[j];
                    }

                    avrg /= sectionN;

                    for (var j = 0; j < sectionN; j++)
                    {
                        x[j] -= avrg;
                    }

                    if (token.IsCancellationRequested) break;
                    // step 5.4
                    for (var j = 0; j < sectionN; j++)
                    {
                        var w = 0.54 - 0.46 * Math.Cos(2 * Math.PI * j / Math.Max(1, sectionN - 1));
                        x[j] *= w;
                    }

                    // step 5.5
                    for (var j = sectionN; j < NN; j++)
                    {
                        x[j] = 0;
                    }

                    if (token.IsCancellationRequested) break;
                    // step 5.6

                    var channel1 = new Channel(x.Length);
                    channel1.SamplingFrq = channel1.SamplingFrq;
                    for (var j = 0; j < x.Length; j++)
                    {
                        channel1.values[j] = x[j];
                    }

                    var analyzer = new Analyzer(myDftCalculator, channel1);
                    analyzer.SetupChannel(0, x.Length);
                    var amps = analyzer.AmplitudeSpectralDensity();

                    int L1 = -(l - 1) / 2, L2 = l / 2;
                    for (var k = 0; k < samplesPerSection && !token.IsCancellationRequested; k++)
                    {
                        var sumAvg = 0.0;
                        for (var j = L1; j <= L2; j++)
                        {
                            sumAvg += amps.values[Math.Abs(l * k + j) % amps.values.Length];
                        }

                        amps.values[k] = sumAvg / l;
                    }

                    if (token.IsCancellationRequested) break;
                    // step 5.7
                    for (int j = 0; j < samplesPerSection; j++)
                    {
                        spectrogramMatrix[j, i] = amps.values[j];
                    }
                }

                countdownEvent.Signal();
            };

            for (int i = 0; i < threadCount; i++)
            {
                ThreadPool.QueueUserWorkItem(threadAction, i);
            }

            try
            {
                countdownEvent.Wait(token);
            }
            catch (OperationCanceledException)
            {
            }

            // step 6
            double maxVal = double.MinValue;
            double minVal = double.MaxValue;

            for (int i = 0; i < sectionsCount; i++)
            {
                for (int j = 0; j < samplesPerSection; j++)
                {
                    maxVal = Math.Max(maxVal, spectrogramMatrix[j, i]);
                    minVal = Math.Min(minVal, spectrogramMatrix[j, i]);
                }
            }

            return new CalculationResult(spectrogramMatrix, minVal, maxVal);
        }
    }
}