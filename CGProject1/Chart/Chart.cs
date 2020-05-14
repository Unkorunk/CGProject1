using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace CGProject1
{
    class Chart : FrameworkElement
    {
        public enum ScalingMode {
            Global,
            Local,
            Fixed
        }

        private double minChannelValue = 0;
        private double maxChannelValue = 0;

        public ScalingMode Scaling { get; set; }

        public double MinFixedScale { get => minChannelValue; set {
                if (this.Scaling == ScalingMode.Fixed)
                {
                    minChannelValue = value;
                    InvalidateVisual();
                }
            }
        }
        public double MaxFixedScale {
            get => maxChannelValue; set {
                if (this.Scaling == ScalingMode.Fixed)
                {
                    minChannelValue = value;
                    InvalidateVisual();
                }
            }
        }

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

        public bool GridDraw { get; set; }

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

            switch (this.Scaling)
            {
                case ScalingMode.Global:
                    {
                        this.minChannelValue = this.channel.MinValue;
                        this.maxChannelValue = this.channel.MaxValue;

                        break;
                    }
                case ScalingMode.Local:
                    {
                        this.minChannelValue = this.channel.MaxValue;
                        this.maxChannelValue = this.channel.MinValue;

                        for (int i = this.Begin; i <= this.End; i++)
                        {
                            this.minChannelValue = Math.Min(this.minChannelValue, this.channel.values[i]);
                            this.maxChannelValue = Math.Max(this.maxChannelValue, this.channel.values[i]);
                        }

                        break;
                    }
                case ScalingMode.Fixed: break;
            }

            double height = Math.Abs(this.maxChannelValue - this.minChannelValue);

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
                    new Point(0, stepY * (1.0 - (prevValueMin - this.minChannelValue) / height) + offsetY),
                    new Point(stepX, stepY * (1.0 - (prevValueMax - this.minChannelValue) / height) + offsetY)
                );

                int n = (this.Length + stepOptimization - 1) / stepOptimization;

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
                        new Point(lineX * stepX, stepY * (1.0 - (prevValueMax - this.minChannelValue) / height) + offsetY),
                        new Point((lineX + 1) * stepX, stepY * (1.0 - (nowValueMin - this.minChannelValue) / height) + offsetY)
                    );
                    dc.DrawLine(
                        new Pen(Brushes.Black, 1.0),
                        new Point((lineX + 1) * stepX, stepY * (1.0 - (nowValueMin - this.minChannelValue) / height) + offsetY),
                        new Point((lineX + 2) * stepX, stepY * (1.0 - (nowValueMax - this.minChannelValue) / height) + offsetY)
                    );

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
                        new Point((i - 1) * stepX, stepY * (1.0 - (prevValue - this.minChannelValue) / height) + offsetY),
                        new Point(i * stepX, stepY * (1.0 - (nowValue - this.minChannelValue) / height) + offsetY)
                    );
                    prevValue = nowValue;
                }
            }

            if (enableSelectInterval) {
                if (optimization) {
                    dc.DrawRectangle(Brushes.LightCoral,
                        new Pen(Brushes.Transparent, 2.0),
                        new Rect(2.0 * stepX * (selectIntervalBegin - this.Begin) / stepOptimization, 0,
                                 2.0 * stepX * (selectIntervalEnd - selectIntervalBegin) / stepOptimization, ActualHeight
                        )
                    );
                } else {
                    dc.DrawRectangle(Brushes.LightCoral,
                        new Pen(Brushes.Transparent, 2.0),
                        new Rect(stepX * (selectIntervalBegin - this.Begin), 0,
                        (selectIntervalEnd - selectIntervalBegin) * stepX, ActualHeight)
                    );
                }
            }

            if (this.GridDraw) {
                for (int i = 0; i < 8; i++) {
                    double x = (i + 1) * this.ActualWidth / 9;
                    dc.DrawLine(new Pen(Brushes.Gray, 1.0), new Point(x, 0), new Point(x, this.ActualHeight));
                    int idx;
                    if (optimization) {
                        idx = (int)Math.Round(x * stepOptimization / (2.0 * stepX) + this.Begin);
                    } else {
                        idx = (int)Math.Round(x / stepX + this.Begin);
                    }

                    var t = this.channel.StartDateTime + TimeSpan.FromSeconds(this.channel.DeltaTime * idx);

                    var formText1 = new FormattedText(t.ToString("dd-MM-yyyy \n hh\\:mm\\:ss") + "\n(" + idx.ToString() + ")",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Times New Roman"),
                        12, Brushes.Blue
                    );
                    formText1.TextAlignment = TextAlignment.Center;

                    dc.DrawText(formText1, new Point(x, 0));
                }

                for (int i = 0; i < 5; i++) {
                    double y = (i + 1) * this.ActualHeight / 6;
                    dc.DrawLine(new Pen(Brushes.Gray, 1.0), new Point(0, y), new Point(this.ActualWidth, y));

                    var formText1 = new FormattedText(Math.Round(this.maxChannelValue - (i + 1) * (this.maxChannelValue - this.minChannelValue) / 6).ToString(),
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Times New Roman"),
                        12, Brushes.Blue
                    );

                    dc.DrawText(formText1, new Point(0, y - formText1.Height / 2));
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
    }
}
