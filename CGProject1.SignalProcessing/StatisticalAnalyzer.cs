namespace CGProject1.SignalProcessing
{
    public class StatisticalAnalyzer
    {
        public class Request
        {
            public readonly int begin;
            public readonly int end;
            public readonly int length;

            public Request(int begin, int end)
            {
                this.begin = begin;
                this.end = end;
                length = end - begin + 1;
            }
        }

        private readonly Channel myChannel;

        public StatisticalAnalyzer(Channel channel)
        {
            myChannel = channel;
        }

        public double CalcAverage(Request request)
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

        public double CalcVariance(Request request, double average)
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

        public double CalcSD(double variance) => Math.Sqrt(variance);

        public double CalcVariability(double sd, double average) => sd / average;

        public double CalcSkewness(Request request, double variance, double average)
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

        public double CalcKurtosis(Request request, double variance, double average)
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

        public double CalcQuantile(Request request, double p)
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

        public double CalcMinValue(Request request)
        {
            if (request.length <= 0) return 0.0;

            double minValue = double.MaxValue;
            for (int i = request.begin; i <= request.end; i++)
            {
                minValue = Math.Min(minValue, myChannel.values[i]);
            }

            return minValue;
        }

        public double CalcMaxValue(Request request)
        {
            if (request.length <= 0) return 0.0;

            var maxValue = double.MinValue;
            for (var i = request.begin; i <= request.end; i++)
            {
                maxValue = Math.Max(maxValue, myChannel.values[i]);
            }

            return maxValue;
        }

        private static void Swap(ref double lhs, ref double rhs)
        {
            (lhs, rhs) = (rhs, lhs);
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

                    Swap(ref arr[left], ref arr[right]);
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