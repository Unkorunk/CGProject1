using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace CGProject1
{
    class Chart : FrameworkElement
    {
        public enum ScalingMode
        {
            Global,
            Local,
            Fixed,
            UniformGlobal,
            UniformLocal
        }

        private double minChannelValue = 0;
        private double maxChannelValue = 0;

        private ScalingMode _scaling;
        public ScalingMode Scaling {
            get => _scaling;
            set
            {
                _scaling = value;
                InvalidateVisual();
            }
        }

        public double MinFixedScale
        {
            get => minChannelValue;
            set
            {
                if (this.Scaling == ScalingMode.Fixed)
                {
                    this.minChannelValue = value;
                    InvalidateVisual();
                }
            }
        }
        public double MaxFixedScale
        {
            get => maxChannelValue;
            set
            {
                if (this.Scaling == ScalingMode.Fixed)
                {
                    this.maxChannelValue = value;
                    InvalidateVisual();
                }
            }
        }
        private List<Chart> _groupedCharts;
        public List<Chart> GroupedCharts { get => _groupedCharts; set
            {
                _groupedCharts = value;
                InvalidateVisual();
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
            get => begin;
            set
            {
                begin = Math.Max(0, Math.Min(value, this.channel.values.Length - 1));
                InvalidateVisual();
            }
        }
        public int End
        {
            get => end;
            set
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
            this._groupedCharts = new List<Chart>() { this };
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (this.Length < 2) return;

            var clipGeomery = new RectangleGeometry(new Rect(0, 0, this.ActualWidth, this.ActualHeight));
            dc.PushClip(clipGeomery);

            #region [Interface] Reserve
            Size interfaceOffset = new Size();

            if (this.GridDraw) {
                var formText1 = new FormattedText(DateTime.Now.ToString("dd-MM-yyyy \n hh\\:mm\\:ss") + "\n(" + int.MaxValue.ToString() + ")",
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                );
                formText1.TextAlignment = TextAlignment.Center;
                interfaceOffset.Height = formText1.Height;

                formText1 = new FormattedText(int.MaxValue.ToString(),
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                );
                interfaceOffset.Width = formText1.Width;
            }

            Size actSize = new Size(this.ActualWidth - interfaceOffset.Width,
                this.ActualHeight - interfaceOffset.Height);
            #endregion [Interface] Reserve

            #region [Optimization]
            const double startOptimizationWith = 1.0;

            double stepX = actSize.Width / (this.Length - 1);
            bool optimization = (stepX < startOptimizationWith);
            int stepOptimization = 0;
            if (optimization)
            {
                stepOptimization = (int)Math.Ceiling(startOptimizationWith * this.Length / actSize.Width);
                stepX *= stepOptimization;
                stepX /= 2.0;
            }

            double stepY = actSize.Height;
            double offsetY = actSize.Height / 2.0 - stepY / 2.0;
            #endregion [Optimization]

            #region [Scaling]
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
                case ScalingMode.UniformGlobal:
                    {
                        this.minChannelValue = this.channel.MinValue;
                        this.maxChannelValue = this.channel.MaxValue;
                        foreach(var chart in GroupedCharts)
                        {
                            this.minChannelValue = Math.Min(this.minChannelValue, chart.channel.MinValue);
                            this.maxChannelValue = Math.Max(this.maxChannelValue, chart.channel.MaxValue);
                        }

                        break;
                    }
                case ScalingMode.UniformLocal:
                    {
                        this.minChannelValue = this.channel.MaxValue;
                        this.maxChannelValue = this.channel.MinValue;

                        for (int i = this.Begin; i <= this.End; i++)
                        {
                            this.minChannelValue = Math.Min(this.minChannelValue, this.channel.values[i]);
                            this.maxChannelValue = Math.Max(this.maxChannelValue, this.channel.values[i]);
                        }

                        foreach (var chart in GroupedCharts)
                        {
                            for (int i = chart.Begin; i <= chart.End; i++)
                            {
                                this.minChannelValue = Math.Min(this.minChannelValue, chart.channel.values[i]);
                                this.maxChannelValue = Math.Max(this.maxChannelValue, chart.channel.values[i]);
                            }
                        }

                        break;
                    }
                case ScalingMode.Fixed: break;
            }

            double height = Math.Abs(this.maxChannelValue - this.minChannelValue);
            #endregion [Scaling]

            #region [Interface] Draw

            #region Background
            dc.DrawRectangle(this.Selected ? Brushes.LightBlue : Brushes.LightGray,
                new Pen(Brushes.DarkGray, 2.0),
                new Rect(interfaceOffset.Width, interfaceOffset.Height, ActualWidth, ActualHeight)
            );
            #endregion Background

            if (this.GridDraw)
            {
                for (int i = 0; i < 8; i++)
                {
                    double x = (i + 1) * actSize.Width / 9;
                    dc.DrawLine(new Pen(Brushes.Gray, 1.0), new Point(interfaceOffset.Width + x, interfaceOffset.Height),
                        new Point(interfaceOffset.Width + x, interfaceOffset.Height + actSize.Height));
                    int idx;
                    if (optimization)
                    {
                        idx = (int)Math.Round(x * stepOptimization / (2.0 * stepX) + this.Begin);
                    }
                    else
                    {
                        idx = (int)Math.Round(x / stepX + this.Begin);
                    }

                    var t = this.channel.StartDateTime + TimeSpan.FromSeconds(this.channel.DeltaTime * idx);

                    var formText1 = new FormattedText(t.ToString("dd-MM-yyyy \n hh\\:mm\\:ss") + "\n(" + idx.ToString() + ")",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Times New Roman"),
                        12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                    );
                    formText1.TextAlignment = TextAlignment.Center;

                    dc.DrawText(formText1, new Point(interfaceOffset.Width + x, 0));
                }

                for (int i = 0; i < 5; i++)
                {
                    double y = (i + 1) * actSize.Height / 6;
                    dc.DrawLine(new Pen(Brushes.Gray, 1.0), new Point(interfaceOffset.Width, interfaceOffset.Height + y),
                        new Point(interfaceOffset.Width + actSize.Width, interfaceOffset.Height + y));
                    var formText1 = new FormattedText(Math.Round(this.maxChannelValue - (i + 1) * (this.maxChannelValue - this.minChannelValue) / 6).ToString(),
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Times New Roman"),
                        12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                    );

                    dc.DrawText(formText1, new Point(0, interfaceOffset.Height + y - formText1.Height / 2));
                }
            }

            #region Channel Name
            var formText = new FormattedText(channel.Name,
                CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight,
                new Typeface("Times New Roman"),
                14, Brushes.Red, VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawRectangle(Brushes.LightGray, new Pen(Brushes.Gray, 1.0), new Rect(0, 0, formText.Width, formText.Height));

            dc.DrawText(formText, new Point(0, 0));
            #endregion Channel Name

            #endregion [Interface] Draw

            #region [Chart] Draw
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
                    new Point(interfaceOffset.Width, interfaceOffset.Height + stepY * (1.0 - (prevValueMin - this.minChannelValue) / height) + offsetY),
                    new Point(interfaceOffset.Width + stepX, interfaceOffset.Height + stepY * (1.0 - (prevValueMax - this.minChannelValue) / height) + offsetY)
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
                        new Point(interfaceOffset.Width + lineX * stepX, interfaceOffset.Height + stepY * (1.0 - (prevValueMax - this.minChannelValue) / height) + offsetY),
                        new Point(interfaceOffset.Width + (lineX + 1) * stepX, interfaceOffset.Height + stepY * (1.0 - (nowValueMin - this.minChannelValue) / height) + offsetY)
                    );
                    dc.DrawLine(
                        new Pen(Brushes.Black, 1.0),
                        new Point(interfaceOffset.Width + (lineX + 1) * stepX, interfaceOffset.Height + stepY * (1.0 - (nowValueMin - this.minChannelValue) / height) + offsetY),
                        new Point(interfaceOffset.Width + (lineX + 2) * stepX, interfaceOffset.Height + stepY * (1.0 - (nowValueMax - this.minChannelValue) / height) + offsetY)
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
                        new Point(interfaceOffset.Width + (i - 1) * stepX, interfaceOffset.Height + stepY * (1.0 - (prevValue - this.minChannelValue) / height) + offsetY),
                        new Point(interfaceOffset.Width + i * stepX, interfaceOffset.Height + stepY * (1.0 - (nowValue - this.minChannelValue) / height) + offsetY)
                    );
                    prevValue = nowValue;
                }
            }
            #endregion [Chart] Draw

            #region [SelectInterval] Draw
            if (enableSelectInterval)
            {
                if (optimization)
                {
                    dc.DrawRectangle(Brushes.LightCoral,
                        new Pen(Brushes.Transparent, 2.0),
                        new Rect(2.0 * stepX * (selectIntervalBegin - this.Begin) / stepOptimization, 0,
                                 2.0 * stepX * (selectIntervalEnd - selectIntervalBegin) / stepOptimization, ActualHeight
                        )
                    );
                }
                else
                {
                    dc.DrawRectangle(Brushes.LightCoral,
                        new Pen(Brushes.Transparent, 2.0),
                        new Rect(stepX * (selectIntervalBegin - this.Begin), 0,
                        (selectIntervalEnd - selectIntervalBegin) * stepX, ActualHeight)
                    );
                }
            }
            #endregion [SelectInterval] Draw
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
