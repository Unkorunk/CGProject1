using System;
using System.Text;
using System.Linq;
using System.Windows;
using CGProject1.Chart;
using System.Windows.Input;
using System.Windows.Controls;

namespace CGProject1
{
    /// <summary>
    /// Interaction logic for StatisticsItem.xaml
    /// </summary>
    public partial class StatisticsItem : UserControl
    {
        private Channel _subscriber;

        private int begin;
        private int end;
        private int length { get => end - begin + 1; }

        public Channel Subscriber
        {
            get => _subscriber;
            set
            {
                _subscriber = value;
                UpdateInfo(0, value.SamplesCount - 1);
            }
        }

        public StatisticsItem(Channel subscriber)
        {
            InitializeComponent();

            Subscriber = subscriber;
        }

        public void UpdateInfo(int begin, int end)
        {
            if (Subscriber == null) return;

            this.begin = begin;
            this.end = end;

            ChannelNameLabel.Content = "Name: " + Subscriber.Name;
            ChannelIntervalLabel.Content = "Begin: " + (this.begin + 1) + "; End: " + (this.end + 1);

            var leftColumn = new StringBuilder();
            var rightColumn = new StringBuilder();

            var average = CalcAverage(Subscriber);
            leftColumn.AppendLine(string.Format("Среднее: {0:0.##}", average));
            var variance = CalcVariance(average, Subscriber);
            leftColumn.AppendLine(string.Format("Дисперсия: {0:0.##}", variance));
            var sd = CalcSD(variance);
            leftColumn.AppendLine(string.Format("Ср.кв.откл: {0:0.##}", sd));
            var variability = CalcVariability(sd, average);
            leftColumn.AppendLine(string.Format("Вариация: {0:0.##}", variability));
            var skewness = CalcSkewness(variance, average, Subscriber);
            leftColumn.AppendLine(string.Format("Асимметрия: {0:0.##}", skewness));
            var kurtosis = CalcKurtosis(variance, average, Subscriber);
            leftColumn.Append(string.Format("Эксцесс: {0:0.##}", kurtosis));

            var minValue = CalcMinValue(Subscriber);
            rightColumn.AppendLine(string.Format("Минимум: {0:0.##}", minValue));
            var maxValue = CalcMaxValue(Subscriber);
            rightColumn.AppendLine(string.Format("Максимум: {0:0.##}", maxValue));
            var quantile5 = CalcQuantile(Subscriber, 0.05);
            rightColumn.AppendLine(string.Format("Квантиль 0.05: {0:0.##}", quantile5));
            var quantile95 = CalcQuantile(Subscriber, 0.95);
            rightColumn.AppendLine(string.Format("Квантиль 0.95: {0:0.##}", quantile95));
            var median = CalcQuantile(Subscriber, 0.5);
            rightColumn.Append(string.Format("Медиана: {0:0.##}", median));

            LeftLabel.Content = leftColumn.ToString();
            RightLabel.Content = rightColumn.ToString();

            Histogram.Data.Clear();

            if (this.length > 0)
            {
                if (int.TryParse(IntervalTextBox.Text, out int K))
                {
                    K = Math.Max(1, K);
                    int[] cnt = new int[K];
                    for (int i = 0; i < this.length; i++)
                    {
                        double p = (Subscriber.values[this.begin + i] - minValue) / (maxValue - minValue);
                        if (Math.Abs(maxValue - minValue) < 1e-6) p = 0.0;
                        cnt[(int)((K - 1) * p)]++;
                    }

                    for (int i = 0; i < K; i++)
                    {
                        Histogram.Data.Add(cnt[i] * 1.0 / this.length);
                    }
                }
            }

            Histogram.InvalidateVisual();
        }

        private void previewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !TextIsNumeric(e.Text);
        }

        private void previewPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string input = (string)e.DataObject.GetData(typeof(string));
                if (!TextIsNumeric(input)) e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void textChanged(object sender, TextChangedEventArgs e) => UpdateInfo(this.begin, this.end);

        private void previewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = (e.Key == Key.Space);
        }

        private bool TextIsNumeric(string input)
        {
            return input.All(c => char.IsDigit(c) || char.IsControl(c));
        }

        public double CalcAverage(Channel chart)
        {
            if (length <= 0) return 0.0;

            double average = 0.0;
            for (int i = this.begin; i <= this.end; i++)
            {
                average += chart.values[i];
            }
            average /= length;

            return average;
        }

        public double CalcVariance(double average, Channel chart)
        {
            if (length <= 0) return 0.0;

            double variance = 0.0;
            for (int i = this.begin; i <= this.end; i++)
            {
                variance += Math.Pow(chart.values[i] - average, 2);
            }
            variance /= length;

            return variance;
        }

        public double CalcSD(double variance) => Math.Sqrt(variance);

        public double CalcVariability(double sd, double average) => sd / average;

        public double CalcSkewness(double variance, double average, Channel chart)
        {
            if (length <= 0) return 0.0;

            double mse3 = Math.Pow(variance, 3.0 / 2.0);

            double skewness = 0.0;
            for (int i = this.begin; i <= this.end; i++)
            {
                skewness += Math.Pow(chart.values[i] - average, 3);
            }
            skewness /= length * mse3;

            return skewness;
        }

        public double CalcKurtosis(double variance, double average, Channel chart)
        {
            if (length <= 0) return 0.0;

            double mse4 = Math.Pow(variance, 2);

            double kurtosis = 0.0;
            for (int i = this.begin; i <= this.end; i++)
            {
                kurtosis += Math.Pow(chart.values[i] - average, 4);
            }
            kurtosis = kurtosis / (length * mse4) - 3.0;

            return kurtosis;
        }

        public double CalcQuantile(Channel chart, double p)
        {
            if (length <= 0) return 0.0;

            p = Math.Clamp(p, 0.0, 1.0);
            int k = (int)(p * (length - 1));

            double[] arr = new double[length];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = chart.values[this.begin + i];
            }

            return OrderStatistics(arr, k);
        }

        public double CalcMinValue(Channel chart)
        {
            if (length <= 0) return 0.0;

            double minValue = double.MaxValue;
            for (int i = this.begin; i <= this.end; i++)
            {
                minValue = Math.Min(minValue, chart.values[i]);
            }

            return minValue;
        }

        public double CalcMaxValue(Channel chart)
        {
            if (length <= 0) return 0.0;

            double maxValue = double.MinValue;
            for (int i = this.begin; i <= this.end; i++)
            {
                maxValue = Math.Max(maxValue, chart.values[i]);
            }

            return maxValue;
        }

        private void Swap(ref double lhs, ref double rhs)
        {
            double t = lhs;
            lhs = rhs;
            rhs = t;
        }

        private int Partition(double[] arr, int left, int right)
        {
            double pivot = arr[left];
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
                    if (arr[left] == arr[right]) return right;

                    Swap(ref arr[left], ref arr[right]);
                }
                else
                {
                    return right;
                }
            }
        }

        private double OrderStatistics(double[] arr, int k)
        {
            int left = 0, right = arr.Length;
            while (true)
            {
                int mid = Partition(arr, left, right - 1);

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
