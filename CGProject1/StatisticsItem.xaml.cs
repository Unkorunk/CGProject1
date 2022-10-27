using System;
using System.Text;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Threading;
using CGProject1.SignalProcessing;

namespace CGProject1
{
    /// <summary>
    /// Interaction logic for StatisticsItem.xaml
    /// </summary>
    public partial class StatisticsItem : IDisposable
    {
        private int begin;
        private int end;
        private int length => end - begin + 1;

        private CancellationTokenSource tokenSource;
        private bool tokenSourceDisposed = true;

        private readonly Channel mySubscriber;
        private readonly StatisticalAnalyzer myAnalyzer;

        public StatisticsItem(Channel subscriber)
        {
            InitializeComponent();

            mySubscriber = subscriber;
            myAnalyzer = new StatisticalAnalyzer(subscriber);
        }

        public void UpdateInfo(int begin, int end)
        {
            if (mySubscriber == null) return;

            if (tokenSource != null)
            {
                tokenSource.Cancel();
                if (!tokenSourceDisposed)
                {
                    tokenSource.Dispose();
                    tokenSourceDisposed = true;
                }
            }

            tokenSource = new CancellationTokenSource();
            tokenSourceDisposed = false;
            var token = tokenSource.Token;

            this.begin = begin;
            this.end = end;

            var request = new StatisticalAnalyzer.Request(begin, end);

            ChannelNameLabel.Content = "Name: " + mySubscriber.Name;
            ChannelIntervalLabel.Content = "Begin: " + (this.begin + 1) + "; End: " + (this.end + 1);

            Task.Factory.StartNew(() =>
            {
                var leftColumn = new StringBuilder();

                if (token.IsCancellationRequested) return string.Empty;
                var average = myAnalyzer.CalcAverage(request);
                leftColumn.AppendLine(string.Format("Среднее: {0:0.##}", average));

                if (token.IsCancellationRequested) return string.Empty;
                var variance = myAnalyzer.CalcVariance(request, average);
                leftColumn.AppendLine(string.Format("Дисперсия: {0:0.##}", variance));

                if (token.IsCancellationRequested) return string.Empty;
                var sd = myAnalyzer.CalcSD(variance);
                leftColumn.AppendLine(string.Format("Ср.кв.откл: {0:0.##}", sd));

                if (token.IsCancellationRequested) return string.Empty;
                var variability = myAnalyzer.CalcVariability(sd, average);
                leftColumn.AppendLine(string.Format("Вариация: {0:0.##}", variability));

                if (token.IsCancellationRequested) return string.Empty;
                var skewness = myAnalyzer.CalcSkewness(request, variance, average);
                leftColumn.AppendLine(string.Format("Асимметрия: {0:0.##}", skewness));

                if (token.IsCancellationRequested) return string.Empty;
                var kurtosis = myAnalyzer.CalcKurtosis(request, variance, average);
                leftColumn.Append(string.Format("Эксцесс: {0:0.##}", kurtosis));

                return leftColumn.ToString();
            }, token).ContinueWith((task) => LeftLabel.Content = task.Result, token,
                TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

            Task.Factory.StartNew(() =>
            {
                var rightColumn = new StringBuilder();

                if (token.IsCancellationRequested) return string.Empty;
                var minValue = myAnalyzer.CalcMinValue(request);
                rightColumn.AppendLine(string.Format("Минимум: {0:0.##}", minValue));

                if (token.IsCancellationRequested) return string.Empty;
                var maxValue = myAnalyzer.CalcMaxValue(request);
                rightColumn.AppendLine(string.Format("Максимум: {0:0.##}", maxValue));

                if (token.IsCancellationRequested) return string.Empty;
                var quantile5 = myAnalyzer.CalcQuantile(request, 0.05);
                rightColumn.AppendLine(string.Format("Квантиль 0.05: {0:0.##}", quantile5));

                if (token.IsCancellationRequested) return string.Empty;
                var quantile95 = myAnalyzer.CalcQuantile(request, 0.95);
                rightColumn.AppendLine(string.Format("Квантиль 0.95: {0:0.##}", quantile95));

                if (token.IsCancellationRequested) return string.Empty;
                var median = myAnalyzer.CalcQuantile(request, 0.5);
                rightColumn.Append(string.Format("Медиана: {0:0.##}", median));

                return rightColumn.ToString();
            }, token).ContinueWith((task) => RightLabel.Content = task.Result, token,
                TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

            if (int.TryParse(IntervalTextBox.Text, out var k))
            {
                k = Math.Max(1, k);

                Task.Factory.StartNew(() =>
                {
                    var minValue = myAnalyzer.CalcMinValue(request);
                    var maxValue = myAnalyzer.CalcMaxValue(request);

                    if (length > 0)
                    {
                        var cnt = new int[k];
                        for (var i = 0; i < length; i++)
                        {
                            if (token.IsCancellationRequested) return null;
                            var p = (mySubscriber.values[this.begin + i] - minValue) / (maxValue - minValue);
                            if (Math.Abs(maxValue - minValue) < 1e-6) p = 0.0;
                            cnt[(int)((k - 1) * p)]++;
                        }

                        var newData = new double[k];
                        for (var i = 0; i < k; i++)
                        {
                            if (token.IsCancellationRequested) return null;
                            newData[i] = cnt[i] * 1.0 / length;
                        }

                        return newData;
                    }

                    return null;
                }, token).ContinueWith((task) =>
                {
                    if (task.Result != null)
                    {
                        Histogram.Data = task.Result;
                        Histogram.InvalidateVisual();
                    }
                }, token, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void IntervalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !TextIsNumeric(e.Text);
        }

        private void IntervalTextBox_PreviewPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var input = (string)e.DataObject.GetData(typeof(string));
                if (!TextIsNumeric(input)) e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void IntervalTextBox_TextChanged(object sender, TextChangedEventArgs e) =>
            UpdateInfo(this.begin, this.end);

        private void IntervalTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = (e.Key == Key.Space);
        }

        private static bool TextIsNumeric(string input)
        {
            return input.All(c => char.IsDigit(c) || char.IsControl(c));
        }

        public void Dispose()
        {
            if (!tokenSourceDisposed)
            {
                tokenSource.Dispose();
                tokenSourceDisposed = true;
            }
        }
    }
}