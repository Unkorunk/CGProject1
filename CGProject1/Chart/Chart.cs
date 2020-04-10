using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CGProject1
{
    class Chart : FrameworkElement
    {
        private Channel channel;

        private int begin = 0;
        private int end = 0;
        public bool Selected { get; private set; }
        public int Begin
        {
            get => begin; set
            {
                begin = Math.Max(0, Math.Min(value, this.channel.values.Length - 1));
                InvalidateVisual();
            }
        }
        public int End
        {
            get => end; set
            {
                end = Math.Max(0, Math.Min(value, this.channel.values.Length - 1));
                InvalidateVisual();
            }
        }

        public int Length { get => End - Begin + 1; }

        public Chart(in Channel channel)
        {
            this.channel = channel;
        }

        protected override void OnRender(DrawingContext dc)
        {
            double stepX = this.ActualWidth / this.Length;
            bool optimization = (stepX < 0.5);
            int stepOptimization = 0;
            if (optimization)
            {
                stepOptimization = (int)Math.Ceiling(0.5 * this.Length / this.ActualWidth);
                stepX *= stepOptimization;
            }

            double stepY = this.ActualHeight;

            double offsetY = this.ActualHeight / 2.0 - stepY / 2.0;

            double channelMinValue = this.channel.MinValue;
            double channelMaxValue = this.channel.MaxValue;

            double height = Math.Abs(channelMaxValue - channelMinValue);

            dc.DrawRectangle(this.Selected ? Brushes.LightBlue : Brushes.LightGray,
                new Pen(Brushes.DarkGray, 2.0), new Rect(0, 0, ActualWidth, ActualHeight));

            if (optimization)
            {
                double prevValueMin = double.MaxValue;
                double prevValueMax = double.MinValue;
                for (int i = 0; i < stepOptimization && this.Begin + i < this.channel.values.Length; i++)
                {
                    prevValueMin = Math.Min(prevValueMin, this.channel.values[this.Begin + i]);
                    prevValueMax = Math.Max(prevValueMax, this.channel.values[this.Begin + i]);
                }
                dc.DrawLine(
                    new Pen(Brushes.Black, 1.0),
                    new Point(0, stepY * (prevValueMin - channelMinValue) / height + offsetY),
                    new Point(stepX / 2.0, stepY * (prevValueMax - channelMinValue) / height + offsetY)
                );
                int lineX = 1;
                int valuesX = 1;

                for (int i = 1; i < this.Length; i += stepOptimization)
                {
                    double nowValueMin = double.MaxValue;
                    double nowValueMax = double.MinValue;
                    for (int j = 0; j < stepOptimization && this.Begin + valuesX * stepOptimization + j < this.channel.values.Length; j++)
                    {
                        nowValueMin = Math.Min(nowValueMin, this.channel.values[this.Begin + valuesX * stepOptimization + j]);
                        nowValueMax = Math.Max(nowValueMax, this.channel.values[this.Begin + valuesX * stepOptimization + j]);
                    }
                    valuesX++;
                    if (Math.Abs(nowValueMax - prevValueMax) < Math.Abs(nowValueMin - prevValueMax))
                    {
                        double t = nowValueMax;
                        nowValueMax = nowValueMin;
                        nowValueMin = t;
                    }

                    dc.DrawLine(
                        new Pen(Brushes.Black, 1.0),
                        new Point(lineX * stepX / 2.0, stepY * (prevValueMax - channelMinValue) / height + offsetY),
                        new Point((lineX + 1) * stepX / 2.0, stepY * (nowValueMin - channelMinValue) / height + offsetY)
                    );
                    dc.DrawLine(
                        new Pen(Brushes.Black, 1.0),
                        new Point((lineX + 1) * stepX / 2.0, stepY * (nowValueMin - channelMinValue) / height + offsetY),
                        new Point((lineX + 2) * stepX / 2.0, stepY * (nowValueMax - channelMinValue) / height + offsetY)
                    );
                    lineX += 2;

                    prevValueMax = nowValueMax;
                }
            }
            else
            {
                double prevValue = this.channel.values[this.Begin];
                for (int i = 1; i < this.Length; i++)
                {
                    double nowValue = this.channel.values[this.Begin + i];
                    dc.DrawLine(
                        new Pen(Brushes.Black, 1.0),
                        new Point((i - 1) * stepX, stepY * (prevValue - channelMinValue) / height + offsetY),
                        new Point(i * stepX, stepY * (nowValue - channelMinValue) / height + offsetY)
                    );
                    prevValue = nowValue;
                }
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            this.Selected = !this.Selected;
            InvalidateVisual();
        }
    }
}
