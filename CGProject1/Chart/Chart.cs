using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CGProject1
{
    class Chart : FrameworkElement
    {
        private Channel channel;

        #region [SelectInterval] Variables
        private bool enableSelectInterval = false;
        private int selectIntervalBegin = 0;
        private int selectIntervalEnd = 0;
        #endregion [SelectInterval] Variables

        private int begin = 0;
        private int end = 0;
        public bool Selected { get; set; }
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
            if (this.Length < 2) return;

            const double startOptimizationWith = 1.0;

            double stepX = this.ActualWidth / (this.Length - 1);
            bool optimization = (stepX < startOptimizationWith);
            int stepOptimization = 0;
            if (optimization)
            {
                stepOptimization = (int)Math.Ceiling(startOptimizationWith * this.Length / this.ActualWidth);
                stepX *= stepOptimization;
                stepX /= 2.0;
            }

            double stepY = this.ActualHeight;

            double offsetY = this.ActualHeight / 2.0 - stepY / 2.0;

            double channelMinValue = this.channel.MinValue;
            double channelMaxValue = this.channel.MaxValue;

            double height = Math.Abs(channelMaxValue - channelMinValue);

            dc.DrawRectangle(this.Selected ? Brushes.LightBlue : Brushes.LightGray,
                new Pen(Brushes.DarkGray, 2.0),
                new Rect(0, 0, ActualWidth, ActualHeight)
            );

            if (optimization)
            {
                double prevValueMin = double.MaxValue;
                double prevValueMax = double.MinValue;
                for (int i = 0; i < stepOptimization; i++)
                {
                    int idx = this.Begin + i;
                    if (idx >= this.channel.values.Length) break;
                    prevValueMin = Math.Min(prevValueMin, this.channel.values[idx]);
                    prevValueMax = Math.Max(prevValueMax, this.channel.values[idx]);
                }
                dc.DrawLine(
                    new Pen(Brushes.Black, 1.0),
                    new Point(0, stepY * (prevValueMin - channelMinValue) / height + offsetY),
                    new Point(stepX, stepY * (prevValueMax - channelMinValue) / height + offsetY)
                );

                int n = (this.Length + stepOptimization - 1) / stepOptimization;
                if (enableSelectInterval)
                {
                    dc.DrawRectangle(Brushes.LightCoral,
                        new Pen(Brushes.Transparent, 2.0),
                        new Rect(2.0 * stepX * (selectIntervalBegin - this.Begin) / stepOptimization, 0,
                                 2.0 * stepX * (selectIntervalEnd - selectIntervalBegin) / stepOptimization, ActualHeight
                        )
                    );
                }

                for (int i = 1; i < n; i++)
                {
                    double nowValueMin = double.MaxValue;
                    double nowValueMax = double.MinValue;
                    for (int j = 0; j < stepOptimization; j++)
                    {
                        int idx = this.Begin + i * stepOptimization + j;
                        if (idx >= this.channel.values.Length) break;

                        nowValueMin = Math.Min(nowValueMin, this.channel.values[idx]);
                        nowValueMax = Math.Max(nowValueMax, this.channel.values[idx]);
                    }
                    if (Math.Abs(nowValueMax - prevValueMax) < Math.Abs(nowValueMin - prevValueMax))
                    {
                        double t = nowValueMax;
                        nowValueMax = nowValueMin;
                        nowValueMin = t;
                    }

                    int lineX = 1 + 2 * (i - 1);
                    dc.DrawLine(
                        new Pen(Brushes.Black, 1.0),
                        new Point(lineX * stepX, stepY * (prevValueMax - channelMinValue) / height + offsetY),
                        new Point((lineX + 1) * stepX, stepY * (nowValueMin - channelMinValue) / height + offsetY)
                    );
                    dc.DrawLine(
                        new Pen(Brushes.Black, 1.0),
                        new Point((lineX + 1) * stepX, stepY * (nowValueMin - channelMinValue) / height + offsetY),
                        new Point((lineX + 2) * stepX, stepY * (nowValueMax - channelMinValue) / height + offsetY)
                    );

                    prevValueMax = nowValueMax;
                }
            }
            else
            {
                if (enableSelectInterval)
                {
                    dc.DrawRectangle(Brushes.LightCoral,
                        new Pen(Brushes.Transparent, 2.0),
                        new Rect((selectIntervalBegin - this.Begin) * stepX, 0, (selectIntervalEnd - selectIntervalBegin) * stepX, ActualHeight)
                    );
                }

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

            var formText = new FormattedText(channel.Name,
CultureInfo.GetCultureInfo("en-us"),
FlowDirection.LeftToRight,
new Typeface("Times New Roman"),
14, Brushes.Red);

            dc.DrawRectangle(Brushes.LightGray, new Pen(Brushes.Gray, 1.0), new Rect(0, 0, formText.Width, formText.Height));

            dc.DrawText(formText, new Point(0, 0));

        }

        public void EnableSelectInterval()
        {
            enableSelectInterval = true;
            InvalidateVisual();
        }
        public void DisableSelectInterval()
        {
            enableSelectInterval = false;
            InvalidateVisual();
        }
        public void SetSelectInterval(int begin, int end)
        {
            selectIntervalBegin = Math.Clamp(begin, this.Begin, this.End);
            selectIntervalEnd = Math.Clamp(end, this.Begin, this.End);
            InvalidateVisual();
        }

        //protected override void OnMouseUp(MouseButtonEventArgs e)
        //{
        //    base.OnMouseUp(e);
        //    this.Selected = !this.Selected;
        //    InvalidateVisual();

            

        //}
    }
}
