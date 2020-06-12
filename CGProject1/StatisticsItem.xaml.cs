using System;
using System.Linq;
using CGProject1.Chart;
using System.Windows.Controls;

namespace CGProject1
{
    /// <summary>
    /// Interaction logic for StatisticsItem.xaml
    /// </summary>
    public partial class StatisticsItem : UserControl
    {
        private ChartLine _subscriber;
        
        public ChartLine Subscriber { 
            get => _subscriber;
            set {
                this._subscriber = value;
                this.UpdateInfo();
            } 
        }

        public StatisticsItem(ChartLine subscriber)
        {
            InitializeComponent();

            this.Subscriber = subscriber;
        }

        public void UpdateInfo()
        {
            if (this.Subscriber == null) return;

            ChannelNameLabel.Content = "Name: " + Subscriber.Channel.Name;
            ChannelIntervalLabel.Content = "Begin: " + (Subscriber.Begin + 1) + "; End: " + (Subscriber.End + 1);

            LeftLabel.Content = "";
            RightLabel.Content = "";

            double avg = CalcAvg(Subscriber);
            LeftLabel.Content += "Среднее: " + Math.Round(avg, 2) + Environment.NewLine;

            double disp = CalcDisp(Subscriber);
            LeftLabel.Content += "Дисперсия: " + Math.Round(disp, 2) + Environment.NewLine;
            LeftLabel.Content += "Ср.кв.откл: " + Math.Round(Math.Sqrt(disp), 2) + Environment.NewLine;

            LeftLabel.Content += "Вариация: " + Math.Round(Math.Sqrt(disp) / avg, 2) + Environment.NewLine;
            LeftLabel.Content += "Асимметрия: " + Math.Round(CalcCoefAsim(Subscriber), 2) + Environment.NewLine;
            LeftLabel.Content += "Эксцесс: " + Math.Round(CalcCoefExces(Subscriber), 2);

            double minValue = Subscriber.Channel.values
                .Where((_, idx) => idx >= Subscriber.Begin && idx <= Subscriber.End)
                .Min();
            RightLabel.Content += "Минимум: " + Math.Round(minValue, 2) + Environment.NewLine;
            double maxValue = Subscriber.Channel.values
                .Where((_, idx) => idx >= Subscriber.Begin && idx <= Subscriber.End)
                .Max();
            RightLabel.Content += "Максимум: " + Math.Round(maxValue, 2) + Environment.NewLine;

            RightLabel.Content += "Квантиль 0.05: " + Math.Round(CalcKvantil(Subscriber, 0.05), 2) + Environment.NewLine;
            RightLabel.Content += "Квантиль 0.95: " + Math.Round(CalcKvantil(Subscriber, 0.95), 2) + Environment.NewLine;
            RightLabel.Content += "Медиана: " + Math.Round(CalcKvantil(Subscriber, 0.5), 2);

            Histogram.Data.Clear();

            if (Subscriber.Length > 0)
            {
                if (int.TryParse(IntervalTextBox.Text, out int K))
                {
                    K = Math.Max(1, K);
                    int[] cnt = new int[K];
                    for (int i = 0; i < Subscriber.Length; i++)
                    {
                        double p = (Subscriber.Channel.values[Subscriber.Begin + i] - minValue) / (maxValue - minValue);
                        if (Math.Abs(maxValue - minValue) < 1e-6) p = 0.0;
                        cnt[(int)((K - 1) * p)]++;
                    }

                    for (int i = 0; i < K; i++)
                    {
                        Histogram.Data.Add(cnt[i] * 1.0 / Subscriber.Length);
                    }
                }
            }

            Histogram.InvalidateVisual();
        }

        public double CalcAvg(ChartLine chart)
        {
            if (chart.Length <= 0) return 0.0;

            return chart.Channel.values
                .Where((_, idx) => idx >= chart.Begin && idx <= chart.End)
                .Average();
        }

        public double CalcDisp(ChartLine chart)
        {
            if (chart.Length <= 0) return 0.0;

            double avg = CalcAvg(chart);
            double disp = 0.0;
            for (int i = chart.Begin; i <= chart.End; i++)
            {
                disp += Math.Pow(chart.Channel.values[i] - avg, 2);
            }
            return disp /= chart.Length;
        }

        public double CalcCoefAsim(ChartLine chart)
        {
            if (chart.Length <= 0) return 0.0;

            double mse3 = Math.Pow(CalcDisp(chart), 3.0 / 2.0);
            double avg = CalcAvg(chart);
            double coef = 0.0;
            for (int i = chart.Begin; i <= chart.End; i++)
            {
                coef += Math.Pow(chart.Channel.values[i] - avg, 3);
            }
            coef /= chart.Length * mse3;
            return coef;
        }

        public double CalcCoefExces(ChartLine chart)
        {
            if (chart.Length <= 0) return 0.0;

            double mse4 = Math.Pow(CalcDisp(chart), 2);
            double avg = CalcAvg(chart);
            double coef = 0.0;
            for (int i = chart.Begin; i <= chart.End; i++)
            {
                coef += Math.Pow(chart.Channel.values[i] - avg, 4);
            }
            coef /= chart.Length * mse4;
            return coef - 3;
        }

        public double CalcKvantil(ChartLine chart, double p)
        {
            if (chart.Length <= 0) return 0.0;

            p = Math.Clamp(p, 0.0, 1.0);
            return chart.Channel.values
                .Where((_, idx) => idx >= chart.Begin && idx <= chart.End)
                .OrderBy(x => x).ToList()[(int)(p * (chart.Length - 1))];
        }

        private void IntervalTextBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateInfo();   
    }
}
