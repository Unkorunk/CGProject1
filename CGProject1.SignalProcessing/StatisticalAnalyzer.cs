namespace CGProject1.SignalProcessing
{
    public class StatisticalAnalyzer
    {
        public struct Request
        {
            public readonly int begin;
            public readonly int end;
            public readonly int length;
            public readonly int k;

            public Request(int begin, int end, int k)
            {
                this.begin = begin;
                this.end = end;
                length = end - begin + 1;
                this.k = k;
            }
        }

        public struct Response
        {
            public readonly double average;
            public readonly double variance;
            public readonly double sd;
            public readonly double variability;
            public readonly double skewness;
            public readonly double kurtosis;
            public readonly double minValue;
            public readonly double maxValue;
            public readonly double quantile5;
            public readonly double quantile95;
            public readonly double median;
            public readonly double[]? histogram;

            public Response(double average, double variance, double sd, double variability, double skewness,
                double kurtosis, double minValue, double maxValue, double quantile5, double quantile95, double median,
                double[]? histogram)
            {
                this.average = average;
                this.variance = variance;
                this.sd = sd;
                this.variability = variability;
                this.skewness = skewness;
                this.kurtosis = kurtosis;
                this.minValue = minValue;
                this.maxValue = maxValue;
                this.quantile5 = quantile5;
                this.quantile95 = quantile95;
                this.median = median;
                this.histogram = histogram;
            }
        }

        private readonly Channel myChannel;

        public StatisticalAnalyzer(Channel channel)
        {
            myChannel = channel;
        }

        public Response GetResponse(Request request)
        {
            var average = CalcAverage(request);
            var variance = CalcVariance(request, average);
            var sd = CalcSD(variance);
            var variability = CalcVariability(sd, average);
            var skewness = CalcSkewness(request, variance, average);
            var kurtosis = CalcKurtosis(request, variance, average);
            var minValue = CalcMinValue(request);
            var maxValue = CalcMaxValue(request);
            var quantile5 = CalcQuantile(request, 0.05);
            var quantile95 = CalcQuantile(request, 0.95);
            var median = CalcQuantile(request, 0.5);
            var histogram = CalcHistogram(request, minValue, maxValue);

            return new Response(average, variance, sd, variability, skewness, kurtosis, minValue, maxValue, quantile5,
                quantile95, median, histogram);
        }

        private double CalcAverage(Request request)
        {
            if (request.length <= 0) return 0.0;

            var average = 0.0;
            for (var i = request.begin; i <= request.end; i++)
            {
                average += myChannel.values[i];
            }

            average /= request.length;

            return average;
        }

        private double CalcVariance(Request request, double average)
        {
            if (request.length <= 0) return 0.0;

            double variance = 0.0;
            for (int i = request.begin; i <= request.end; i++)
            {
                variance += Math.Pow(myChannel.values[i] - average, 2);
            }

            variance /= request.length;

            return variance;
        }

        private double CalcSD(double variance) => Math.Sqrt(variance);

        private double CalcVariability(double sd, double average) => sd / average;

        private double CalcSkewness(Request request, double variance, double average)
        {
            if (request.length <= 0) return 0.0;

            double mse3 = Math.Pow(variance, 3.0 / 2.0);

            double skewness = 0.0;
            for (int i = request.begin; i <= request.end; i++)
            {
                skewness += Math.Pow(myChannel.values[i] - average, 3);
            }

            skewness /= request.length * mse3;

            return skewness;
        }

        private double CalcKurtosis(Request request, double variance, double average)
        {
            if (request.length <= 0) return 0.0;

            double mse4 = Math.Pow(variance, 2);

            double kurtosis = 0.0;
            for (int i = request.begin; i <= request.end; i++)
            {
                kurtosis += Math.Pow(myChannel.values[i] - average, 4);
            }

            kurtosis = kurtosis / (request.length * mse4) - 3.0;

            return kurtosis;
        }

        private double CalcQuantile(Request request, double p)
        {
            if (request.length <= 0) return 0.0;

            p = Math.Clamp(p, 0.0, 1.0);
            int k = (int)(p * (request.length - 1));

            double[] arr = new double[request.length];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = myChannel.values[request.begin + i];
            }

            return OrderStatistics(arr, k, 1e-7);
        }

        private double CalcMinValue(Request request)
        {
            if (request.length <= 0) return 0.0;

            double minValue = double.MaxValue;
            for (int i = request.begin; i <= request.end; i++)
            {
                minValue = Math.Min(minValue, myChannel.values[i]);
            }

            return minValue;
        }

        private double CalcMaxValue(Request request)
        {
            if (request.length <= 0) return 0.0;

            var maxValue = double.MinValue;
            for (var i = request.begin; i <= request.end; i++)
            {
                maxValue = Math.Max(maxValue, myChannel.values[i]);
            }

            return maxValue;
        }

        private double[]? CalcHistogram(Request request, double minValue, double maxValue)
        {
            if (request.length > 0)
            {
                var cnt = new int[request.k];
                for (var i = 0; i < request.length; i++)
                {
                    var p = (myChannel.values[request.begin + i] - minValue) / (maxValue - minValue);
                    if (Math.Abs(maxValue - minValue) < 1e-6) p = 0.0;
                    cnt[(int)((request.k - 1) * p)]++;
                }

                var newData = new double[request.k];
                for (var i = 0; i < request.k; i++)
                {
                    newData[i] = cnt[i] * 1.0 / request.length;
                }

                return newData;
            }

            return null;
        }

        private static int Partition(double[] arr, int left, int right, double eps)
        {
            var pivot = arr[left];
            while (true)
            {
                while (arr[left] < pivot)
                {
                    left++;
                }

                while (arr[right] > pivot)
                {
                    right--;
                }

                if (left < right)
                {
                    if (Math.Abs(arr[left] - arr[right]) < eps) return right;

                    (arr[left], arr[right]) = (arr[right], arr[left]);
                }
                else
                {
                    return right;
                }
            }
        }

        private static double OrderStatistics(double[] arr, int k, double eps)
        {
            int left = 0, right = arr.Length;
            while (true)
            {
                var mid = Partition(arr, left, right - 1, eps);

                if (mid == k)
                {
                    return arr[mid];
                }
                else if (k < mid)
                {
                    right = mid;
                }
                else
                {
                    left = mid + 1;
                }
            }
        }
    }
}