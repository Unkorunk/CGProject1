using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CGProject1
{
    class Chart : FrameworkElement
    {
        public delegate void OnMouseSelectDel(Chart sender, int newBegin, int newEnd);
        public OnMouseSelectDel OnMouseSelect = (a, b, c) => { };

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
        public bool IsMouseSelect { get; set; }

        private bool enableSelectInterval = false;
        private int selectIntervalBegin = 0;
        private int selectIntervalEnd = 0;

        private int fakeBegin = 0;
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

        private Size interfaceOffset = new Size();
        private bool optimization = false;
        private double stepX = 1.0;
        private int stepOptimization = 0;

        public Chart(in Channel channel)
        {
            this.channel = channel;
            this._groupedCharts = new List<Chart>() { this };
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (this.Length < 2) return;

            var clipGeomery = new RectangleGeometry(new Rect(0, 0, this.ActualWidth, this.ActualHeight));
            dc.PushClip(clipGeomery);

            #region [Interface] Reserve
            interfaceOffset = new Size();

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
            } else
            {
                var formText1 = new FormattedText("(" + int.MaxValue.ToString() + ")",
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                );
                formText1.TextAlignment = TextAlignment.Center;
                interfaceOffset.Height = formText1.Height;
            }

            Size actSize = new Size(this.ActualWidth - interfaceOffset.Width,
                this.ActualHeight - interfaceOffset.Height);
            #endregion [Interface] Reserve

            #region [Optimization]
            const double startOptimizationWith = 1.0;

            stepX = actSize.Width / (this.Length - 1);
            optimization = (stepX < startOptimizationWith);
            stepOptimization = 0;
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
                new Rect(interfaceOffset.Width, interfaceOffset.Height, actSize.Width, actSize.Height)
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


            dc.DrawText(formText, new Point(this.GridDraw ? 0 : this.ActualWidth / 2 - formText.Width / 2, 0));
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
                var brush = new SolidColorBrush(Color.FromArgb(100, 255, 153, 51));
                if (optimization)
                {
                    dc.DrawRectangle(brush,
                        new Pen(Brushes.Transparent, 2.0),
                        new Rect(interfaceOffset.Width + 2.0 * stepX * (selectIntervalBegin - this.Begin) / stepOptimization,
                                 interfaceOffset.Height,
                                 2.0 * stepX * (selectIntervalEnd - selectIntervalBegin) / stepOptimization,
                                 actSize.Height
                        )
                    );
                }
                else
                {
                    dc.DrawRectangle(brush,
                        new Pen(Brushes.Transparent, 2.0),
                        new Rect(interfaceOffset.Width + stepX * (selectIntervalBegin - this.Begin),
                                 interfaceOffset.Height,
                                 (selectIntervalEnd - selectIntervalBegin) * stepX,
                                 actSize.Height)
                    );
                }
            }
            #endregion [SelectInterval] Draw
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            var position = e.GetPosition(this);
            if (e.ChangedButton == MouseButton.Left && this.IsMouseSelect &&
                position.X >= interfaceOffset.Width &&
                position.Y >= interfaceOffset.Height)
            {
                int idx = GetIdx(position);
                selectIntervalBegin = selectIntervalEnd = Math.Clamp(idx, this.Begin, this.End);
                fakeBegin = selectIntervalBegin;

                enableSelectInterval = true;
                InvalidateVisual();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            var position = e.GetPosition(this);
            if (e.ChangedButton == MouseButton.Left && this.IsMouseSelect &&
                position.X >= interfaceOffset.Width &&
                position.Y >= interfaceOffset.Height)
            {
                int idx = GetIdx(position);

                selectIntervalEnd = idx;
                selectIntervalBegin = fakeBegin;
                if (selectIntervalEnd < selectIntervalBegin)
                {
                    selectIntervalBegin = idx;
                    selectIntervalEnd = fakeBegin;
                }

                selectIntervalBegin = Math.Clamp(selectIntervalBegin, this.Begin, this.End);
                selectIntervalEnd = Math.Clamp(selectIntervalEnd, this.Begin, this.End);

                enableSelectInterval = false;

                if (selectIntervalEnd > selectIntervalBegin)
                {
                    this.Begin = selectIntervalBegin;
                    this.End = selectIntervalEnd;
                    OnMouseSelect(this, this.Begin, this.End);
                }

                InvalidateVisual();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var position = e.GetPosition(this);
            if (position.X >= interfaceOffset.Width &&
                position.Y >= interfaceOffset.Height &&
                enableSelectInterval && this.IsMouseSelect)
            {
                int idx = GetIdx(position);

                selectIntervalEnd = idx;
                selectIntervalBegin = fakeBegin;
                if (selectIntervalEnd < selectIntervalBegin)
                {
                    selectIntervalBegin = idx;
                    selectIntervalEnd = fakeBegin;
                }

                selectIntervalBegin = Math.Clamp(selectIntervalBegin, this.Begin, this.End);
                selectIntervalEnd = Math.Clamp(selectIntervalEnd, this.Begin, this.End);

                InvalidateVisual();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            if (this.IsMouseSelect)
            {
                enableSelectInterval = false;
                InvalidateVisual();
            }
        }

        private int GetIdx(Point position)
        {
            position.X -= interfaceOffset.Width;
            position.Y -= interfaceOffset.Height;

            int idx;
            if (optimization)
            {
                idx = (int)Math.Round(position.X * stepOptimization / (2.0 * stepX) + this.Begin);
            }
            else
            {
                idx = (int)Math.Round(position.X / stepX + this.Begin);
            }
            return idx;
        }

        //public void EnableSelectInterval()
        //{
        //    enableSelectInterval = true;
        //    InvalidateVisual();
        //}
        //public void DisableSelectInterval()
        //{
        //    enableSelectInterval = false;
        //    InvalidateVisual();
        //}
        //public void SetSelectInterval(int begin, int end)
        //{
        //    selectIntervalBegin = Math.Clamp(begin, this.Begin, this.End);
        //    selectIntervalEnd = Math.Clamp(end, this.Begin, this.End);
        //    InvalidateVisual();
        //}
    }
}
