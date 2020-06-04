using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CGProject1
{
    public class Chart : FrameworkElement
    {
        public delegate void OnMouseSelectDel(Chart sender, int newBegin, int newEnd);
        public OnMouseSelectDel OnMouseSelect = (a, b, c) => { };
        public delegate void OnChangeIntervalDel(Chart sender);
        public OnChangeIntervalDel OnChangeInterval = (sender) => { };

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
        public ScalingMode Scaling
        {
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
        public List<Chart> GroupedCharts
        {
            get => _groupedCharts; set
            {
                _groupedCharts = value;
                InvalidateVisual();
            }
        }

        public bool DisplayHAxisInfo { get; set; }
        public bool DisplayVAxisInfo { get; set; }
        public bool DisplayTitle { get; set; }
        public int MaxVAxisLength { get; set; }

        public bool DisplayHAxisTitle { get; set; }
        public string HAxisTitle { get; set; }
        
        public enum HAxisAlligmentEnum
        {
            Top, Bottom
        }
        public HAxisAlligmentEnum HAxisAlligment { get; set; }

        public Channel Channel { get; }

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
                if (value <= end)
                {
                    begin = Math.Max(0, Math.Min(value, this.Channel.values.Length - 1));

                    InvalidateVisual();
                    OnChangeInterval(this);
                }
            }
        }
        public int End
        {
            get => end;
            set
            {
                if (value >= begin)
                {
                    end = Math.Max(0, Math.Min(value, this.Channel.values.Length - 1));

                    InvalidateVisual();
                    OnChangeInterval(this);
                }
            }
        }

        public bool GridDraw { get; set; }

        public int Length { get => End - Begin + 1; }

        public bool ShowCurrentXY { get; set; }

        private Size interfaceOffset = new Size();
        private bool optimization = false;
        private double stepX = 1.0;
        private int stepOptimization = 0;

        private ToolTip tooltip;

        private int curSelected = -1;

        public Chart(in Channel channel)
        {
            this.Channel = channel;
            this._groupedCharts = new List<Chart>() { this };
            this.tooltip = new ToolTip();
            this.tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            this.tooltip.PlacementTarget = this;

            DisplayHAxisInfo = true;
            DisplayVAxisInfo = true;
            DisplayTitle = true;

            MaxVAxisLength = 12;
        }

        public Func<int, Chart, string> MappingXAxis = (idx, chart) => {

            var t = chart.Channel.StartDateTime + TimeSpan.FromSeconds(chart.Channel.DeltaTime * idx);
            return t.ToString("dd-MM-yyyy \n HH\\:mm\\:ss") + "\n(" + idx.ToString() + ")";
        };
        public string MaxHeightXAxisString = DateTime.Now.ToString("dd-MM-yyyy \n hh\\:mm\\:ss") + "\n(" + int.MaxValue.ToString() + ")";

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (this.Length < 2) return;

            var clipGeomery = new RectangleGeometry(new Rect(0, 0, this.ActualWidth, this.ActualHeight));
            dc.PushClip(clipGeomery);

            #region [Interface] Reserve
            interfaceOffset = new Size();

            if (GridDraw)
            {
                if (DisplayHAxisTitle)
                {
                    var formText3 = new FormattedText(HAxisTitle,
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Times New Roman"),
                        12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                    );

                    formText3.TextAlignment = TextAlignment.Center;
                    interfaceOffset.Height = formText3.Height + 1;
                }

                if (DisplayHAxisInfo)
                {
                    var formText1 = new FormattedText(MaxHeightXAxisString,
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Times New Roman"),
                        12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                    );

                    formText1.TextAlignment = TextAlignment.Center;
                    interfaceOffset.Height += formText1.Height + 1;
                }

                if (DisplayVAxisInfo)
                {
                    var formText2 = new FormattedText(new string('7', this.MaxVAxisLength),
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Times New Roman"),
                        12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                    );
                    interfaceOffset.Width = formText2.Width;
                }
            }
            else if (DisplayTitle)
            {
                var formText1 = new FormattedText(this.Channel.Name,
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                );
                formText1.TextAlignment = TextAlignment.Center;
                interfaceOffset.Height = formText1.Height + 1;
            }

            Size actSize = new Size(this.ActualWidth - interfaceOffset.Width,
                this.ActualHeight - interfaceOffset.Height);

            if ((DisplayHAxisTitle || DisplayHAxisInfo) && HAxisAlligment == HAxisAlligmentEnum.Bottom)
            {
                interfaceOffset.Height = 0;
            }
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
                        this.minChannelValue = this.Channel.MinValue;
                        this.maxChannelValue = this.Channel.MaxValue;

                        break;
                    }
                case ScalingMode.Local:
                    {
                        this.minChannelValue = this.Channel.MaxValue;
                        this.maxChannelValue = this.Channel.MinValue;

                        for (int i = this.Begin; i <= this.End; i++)
                        {
                            this.minChannelValue = Math.Min(this.minChannelValue, this.Channel.values[i]);
                            this.maxChannelValue = Math.Max(this.maxChannelValue, this.Channel.values[i]);
                        }

                        break;
                    }
                case ScalingMode.UniformGlobal:
                    {
                        this.minChannelValue = this.Channel.MinValue;
                        this.maxChannelValue = this.Channel.MaxValue;
                        foreach (var chart in GroupedCharts)
                        {
                            this.minChannelValue = Math.Min(this.minChannelValue, chart.Channel.MinValue);
                            this.maxChannelValue = Math.Max(this.maxChannelValue, chart.Channel.MaxValue);
                        }

                        break;
                    }
                case ScalingMode.UniformLocal:
                    {
                        this.minChannelValue = this.Channel.MaxValue;
                        this.maxChannelValue = this.Channel.MinValue;

                        for (int i = this.Begin; i <= this.End; i++)
                        {
                            this.minChannelValue = Math.Min(this.minChannelValue, this.Channel.values[i]);
                            this.maxChannelValue = Math.Max(this.maxChannelValue, this.Channel.values[i]);
                        }

                        foreach (var chart in GroupedCharts)
                        {
                            for (int i = chart.Begin; i <= chart.End; i++)
                            {
                                this.minChannelValue = Math.Min(this.minChannelValue, chart.Channel.values[i]);
                                this.maxChannelValue = Math.Max(this.maxChannelValue, chart.Channel.values[i]);
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

            if (GridDraw)
            {
                var ht = 0.0;
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

                    if (DisplayHAxisInfo)
                    {
                        string XAxisText = MappingXAxis(idx, this);

                        var formText1 = new FormattedText(XAxisText,
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Times New Roman"),
                            12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                        );
                        formText1.TextAlignment = TextAlignment.Center;

                        if (HAxisAlligment == HAxisAlligmentEnum.Bottom)
                        {
                            var offsetY1 = actSize.Height;
                            ht = Math.Max(ht, offsetY1 + formText1.Height);
                            dc.DrawText(formText1, new Point(interfaceOffset.Width + x, offsetY1));
                        }
                        else
                        {
                            dc.DrawText(formText1, new Point(interfaceOffset.Width + x, interfaceOffset.Height - (formText1.Height + 1)));
                        }  
                    }
                }

                if (DisplayHAxisTitle)
                {
                    var formText3 = new FormattedText(HAxisTitle,
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Times New Roman"),
                        12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                    );

                    formText3.TextAlignment = TextAlignment.Center;
                    
                    dc.DrawText(formText3, new Point(interfaceOffset.Width + actSize.Width / 2, ht));
                }

                for (int i = 0; i < 5; i++)
                {
                    double y = (i + 1) * actSize.Height / 6;
                    dc.DrawLine(new Pen(Brushes.Gray, 1.0), new Point(interfaceOffset.Width, interfaceOffset.Height + y),
                        new Point(interfaceOffset.Width + actSize.Width, interfaceOffset.Height + y));
                    string val = Math.Round(this.maxChannelValue - (i + 1) * (this.maxChannelValue - this.minChannelValue) / 6, 5).ToString(CultureInfo.InvariantCulture);
                    if (val.Length > 8)
                    {
                        val = val.Substring(0, 8);
                    }
                    if (DisplayVAxisInfo)
                    {
                        if (val.Length > 12)
                        {
                            val = val.Substring(0, 12);
                        }

                        var formText1 = new FormattedText(val,
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Times New Roman"),
                            12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                        );

                        formText1.TextAlignment = TextAlignment.Right;

                        dc.DrawText(formText1, new Point(interfaceOffset.Width - 5, interfaceOffset.Height + y - formText1.Height / 2));
                    }
                }
            }

            #region Channel Name
            if (DisplayTitle)
            {
                var formText = new FormattedText(Channel.Name,
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    14, Brushes.Red, VisualTreeHelper.GetDpi(this).PixelsPerDip);


                dc.DrawText(formText, new Point(this.GridDraw ? 0 : this.ActualWidth / 2 - formText.Width / 2, 0));
            }
            #endregion Channel Name

            #endregion [Interface] Draw

            #region [Chart] Draw
            {
                int idx = 0;
                double prevValue = double.NaN;
                foreach(var nowValue in Optimize(stepOptimization))
                {
                    if (idx != 0)
                    {
                        dc.DrawLine(
                            new Pen(Brushes.Black, 1.0),
                            new Point(interfaceOffset.Width + (idx - 1) * stepX, interfaceOffset.Height + stepY * (1.0 - (prevValue - this.minChannelValue) / height) + offsetY),
                            new Point(interfaceOffset.Width + idx * stepX, interfaceOffset.Height + stepY * (1.0 - (nowValue - this.minChannelValue) / height) + offsetY)
                        );
                    }

                    prevValue = nowValue;
                    idx++;
                }
            }
            #endregion [Chart] Draw

            #region [SelectInterval] Draw
            if (this.curSelected != -1)
            {
                if (optimization)
                {
                    dc.DrawLine(new Pen(Brushes.Green, 2.0),
                        new Point(interfaceOffset.Width + 2.0 * stepX * (this.curSelected - this.Begin) / stepOptimization, interfaceOffset.Height),
                        new Point(interfaceOffset.Width + 2.0 * stepX * (this.curSelected - this.Begin) / stepOptimization, actSize.Height + interfaceOffset.Height));
                }
                else
                {
                    dc.DrawLine(new Pen(Brushes.Green, 2.0),
                        new Point(interfaceOffset.Width + stepX * (this.curSelected - this.Begin), interfaceOffset.Height),
                        new Point(interfaceOffset.Width + stepX * (this.curSelected - this.Begin), actSize.Height + interfaceOffset.Height));
                }
            }

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
                position.Y >= interfaceOffset.Height)
            {


                if (ShowCurrentXY)
                {
                    this.curSelected = GetIdx(position);
                    this.tooltip.IsOpen = true;
                    this.tooltip.HorizontalOffset = 1;//position.X;
                    this.tooltip.VerticalOffset = this.ActualHeight - 26;// position.Y - 20;
                    this.tooltip.Content = $"X: {this.curSelected}; Y: {this.Channel.values[this.curSelected]}";
                }

                if (enableSelectInterval && this.IsMouseSelect)
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
                }

                InvalidateVisual();
            }
            else if (this.curSelected != -1)
            {
                this.tooltip.IsOpen = false;
                this.curSelected = -1;
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (ShowCurrentXY)
            {
                if (this.curSelected != -1)
                {
                    this.tooltip.IsOpen = false;
                    this.curSelected = -1;
                }
            }


            if (this.IsMouseSelect)
            {
                enableSelectInterval = false;
            }
            InvalidateVisual();
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

        public IEnumerable<double> Optimize(int step)
        {
            if (step < 3)
            {
                for (int i = this.Begin; i <= this.End; i++)
                {
                    yield return this.Channel.values[i];
                }
                yield break;
            }

            double prevMaxValue = double.NaN;

            int iterations = (this.Length + step - 1) / step;
            for (int i = 0; i < iterations; i++)
            {
                double minValue = double.MaxValue;
                double maxValue = double.MinValue;
                for (int j = 0; j < step; j++)
                {
                    int idx = this.Begin + i * step + j;
                    if (idx > this.End) break;

                    minValue = Math.Min(minValue, this.Channel.values[idx]);
                    maxValue = Math.Max(maxValue, this.Channel.values[idx]);
                }

                if (i != 0 && Math.Abs(maxValue - prevMaxValue) < Math.Abs(minValue - prevMaxValue))
                {
                    double t = maxValue;
                    maxValue = minValue;
                    minValue = t;
                }
                prevMaxValue = maxValue;

                yield return minValue;
                yield return maxValue;
            }
        }
    }
}
