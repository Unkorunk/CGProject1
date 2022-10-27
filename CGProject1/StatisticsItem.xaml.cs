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
    public partial class StatisticsItem
    {
        private int begin;
        private int end;
        private int length => end - begin + 1;

        private readonly Channel mySubscriber;
        private readonly StatisticalAnalyzer myAnalyzer;

        public StatisticsItem(Channel subscriber)
        {
            InitializeComponent();

            mySubscriber = subscriber;
            myAnalyzer = new StatisticalAnalyzer(subscriber);
        }

        public async Task UpdateInfo(int begin, int end)
        {
            if (mySubscriber == null) return;

            this.begin = begin;
            this.end = end;

            if (!int.TryParse(IntervalTextBox.Text, out var k))
                return;
            k = Math.Max(1, k);

            var request = new StatisticalAnalyzer.Request(begin, end, k);
            var response = await Task.Run(() => myAnalyzer.GetResponse(request));

            ChannelNameLabel.Content = "Name: " + mySubscriber.Name;
            ChannelIntervalLabel.Content = "Begin: " + (this.begin + 1) + "; End: " + (this.end + 1);

            var leftColumn = new StringBuilder();
            leftColumn.AppendLine(string.Format("Среднее: {0:0.##}", response.average));
            leftColumn.AppendLine(string.Format("Дисперсия: {0:0.##}", response.variance));
            leftColumn.AppendLine(string.Format("Ср.кв.откл: {0:0.##}", response.sd));
            leftColumn.AppendLine(string.Format("Вариация: {0:0.##}", response.variability));
            leftColumn.AppendLine(string.Format("Асимметрия: {0:0.##}", response.skewness));
            leftColumn.Append(string.Format("Эксцесс: {0:0.##}", response.kurtosis));
            LeftLabel.Content = leftColumn.ToString();

            var rightColumn = new StringBuilder();
            rightColumn.AppendLine(string.Format("Минимум: {0:0.##}", response.minValue));
            rightColumn.AppendLine(string.Format("Максимум: {0:0.##}", response.maxValue));
            rightColumn.AppendLine(string.Format("Квантиль 0.05: {0:0.##}", response.quantile5));
            rightColumn.AppendLine(string.Format("Квантиль 0.95: {0:0.##}", response.quantile95));
            rightColumn.Append(string.Format("Медиана: {0:0.##}", response.median));
            RightLabel.Content = rightColumn.ToString();

            Histogram.Data = response.histogram;
            Histogram.InvalidateVisual();
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

        private async void IntervalTextBox_TextChanged(object sender, TextChangedEventArgs e) =>
            await UpdateInfo(this.begin, this.end);

        private void IntervalTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = (e.Key == Key.Space);
        }

        private static bool TextIsNumeric(string input)
        {
            return input.All(c => char.IsDigit(c) || char.IsControl(c));
        }
    }
}