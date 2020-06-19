using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CGProject1.Chart
{
    public class ChartLine : FrameworkElement
    {
        public delegate void OnMouseSelectDel(ChartLine sender, int newBegin, int newEnd);
        public OnMouseSelectDel OnMouseSelect = (a, b, c) => { };
        public delegate void OnChangeIntervalDel(ChartLine sender);
        public OnChangeIntervalDel OnChangeInterval = (sender) => { };

        public enum ScalingMode
        {
            Global,
            Local,
            Fixed,
            UniformGlobal,
            UniformLocal,
            LocalZeroed
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
                if (Scaling == ScalingMode.Fixed)
                {
                    minChannelValue = value;
                    InvalidateVisual();
                }
            }
        }
        public double MaxFixedScale
        {
            get => maxChannelValue;
            set
            {
                if (Scaling == ScalingMode.Fixed)
                {
                    maxChannelValue = value;
                    InvalidateVisual();
                }
            }
        }
        private List<ChartLine> _groupedCharts;
        public List<ChartLine> GroupedCharts
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
                    begin = Math.Max(0, Math.Min(value, Channel.values.Length - 1));

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
                    end = Math.Max(0, Math.Min(value, Channel.values.Length - 1));

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

        //private ToolTip tooltip;

        private int curSelected = -1;

        public ChartLine(in Channel channel)
        {
            Channel = channel;
            _groupedCharts = new List<ChartLine>() { this };
            //tooltip = new ToolTip();
            //tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            //tooltip.PlacementTarget = this;

            DisplayHAxisInfo = true;
            DisplayVAxisInfo = true;
            DisplayTitle = true;

            MaxVAxisLength = 12;
        }

        public Func<int, ChartLine, string> MappingXAxis = (idx, chart) =>
        {

            var t = chart.Channel.StartDateTime + TimeSpan.FromSeconds(chart.Channel.DeltaTime * idx);
            return t.ToString("dd-MM-yyyy \n HH\\:mm\\:ss") + "\n(" + idx.ToString() + ")";
        };
        public string MaxHeightXAxisString = DateTime.Now.ToString("dd-MM-yyyy \n HH\\:mm\\:ss") + "\n(" + int.MaxValue.ToString() + ")";

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (Length < 2) return;

            var clipGeomery = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
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
                    var formText2 = new FormattedText(new string('7', MaxVAxisLength),
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
                var formText1 = new FormattedText(Channel.Name,
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                );
                formText1.TextAlignment = TextAlignment.Center;
                interfaceOffset.Height = formText1.Height + 1;
            }

            Size actSize = new Size(ActualWidth - interfaceOffset.Width,
                ActualHeight - interfaceOffset.Height);

            if ((DisplayHAxisTitle || DisplayHAxisInfo) && HAxisAlligment == HAxisAlligmentEnum.Bottom)
            {
                interfaceOffset.Height = 0;
            }
            #endregion [Interface] Reserve

            #region [Optimization]
            const double startOptimizationWith = 1.0;

            stepX = actSize.Width / (Length - 1);
            optimization = stepX < startOptimizationWith;
            stepOptimization = 0;
            if (optimization)
            {
                stepOptimization = (int)Math.Ceiling(startOptimizationWith * Length / actSize.Width);
                stepX *= stepOptimization;
                stepX /= 2.0;
            }

            double stepY = actSize.Height;
            double offsetY = actSize.Height / 2.0 - stepY / 2.0;
            #endregion [Optimization]

            #region [Scaling]
            switch (Scaling)
            {
                case ScalingMode.Global:
                    {
                        minChannelValue = Channel.MinValue;
                        maxChannelValue = Channel.MaxValue;

                        break;
                    }
                case ScalingMode.Local:
                    {
                        minChannelValue = Channel.MaxValue;
                        maxChannelValue = Channel.MinValue;

                        for (int i = Begin; i <= End; i++)
                        {
                            minChannelValue = Math.Min(minChannelValue, Channel.values[i]);
                            maxChannelValue = Math.Max(maxChannelValue, Channel.values[i]);
                        }

                        break;
                    }
                case ScalingMode.UniformGlobal:
                    {
                        minChannelValue = Channel.MinValue;
                        maxChannelValue = Channel.MaxValue;
                        foreach (var chart in GroupedCharts)
                        {
                            minChannelValue = Math.Min(minChannelValue, chart.Channel.MinValue);
                            maxChannelValue = Math.Max(maxChannelValue, chart.Channel.MaxValue);
                        }

                        break;
                    }
                case ScalingMode.UniformLocal:
                    {
                        minChannelValue = Channel.MaxValue;
                        maxChannelValue = Channel.MinValue;

                        for (int i = Begin; i <= End; i++)
                        {
                            minChannelValue = Math.Min(minChannelValue, Channel.values[i]);
                            maxChannelValue = Math.Max(maxChannelValue, Channel.values[i]);
                        }

                        foreach (var chart in GroupedCharts)
                        {
                            for (int i = chart.Begin; i <= chart.End; i++)
                            {
                                minChannelValue = Math.Min(minChannelValue, chart.Channel.values[i]);
                                maxChannelValue = Math.Max(maxChannelValue, chart.Channel.values[i]);
                            }
                        }

                        break;
                    }
                case ScalingMode.Fixed: break;
                case ScalingMode.LocalZeroed:
                    {
                        minChannelValue = 0;
                        maxChannelValue = Channel.MinValue;

                        for (int i = Begin; i <= End; i++)
                        {
                            maxChannelValue = Math.Max(maxChannelValue, Channel.values[i]);
                        }

                        break;
                    }
            }

            double height = Math.Abs(maxChannelValue - minChannelValue);
            #endregion [Scaling]

            #region [Interface] Draw

            #region Background
            dc.DrawRectangle(Selected ? Brushes.LightBlue : Brushes.LightGray,
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
                        idx = (int)Math.Round(x * stepOptimization / (2.0 * stepX) + Begin);
                    }
                    else
                    {
                        idx = (int)Math.Round(x / stepX + Begin);
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
                    string val = Math.Round(maxChannelValue - (i + 1) * (maxChannelValue - minChannelValue) / 6, 5).ToString(CultureInfo.InvariantCulture);
                    if (val.Length > this.MaxVAxisLength)
                    {
                        val = val.Substring(0, this.MaxVAxisLength);
                    }
                    if (DisplayVAxisInfo)
                    {
                        var formText1 = new FormattedText(val,
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Times New Roman"),
                            12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                        );

                        formText1.TextAlignment = TextAlignment.Right;

                        dc.DrawText(formText1, new Point(interfaceOffset.Width, interfaceOffset.Height + y - formText1.Height / 2));
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


                dc.DrawText(formText, new Point(GridDraw ? 0 : ActualWidth / 2 - formText.Width / 2, 0));
            }
            #endregion Channel Name

            #endregion [Interface] Draw

            #region [Chart] Draw
            {
                int idx = 0;
                double prevValue = double.NaN;
                foreach (var nowValue in Optimize(stepOptimization))
                {
                    if (idx != 0)
                    {
                        dc.DrawLine(
                            new Pen(Brushes.Black, 1.0),
                            new Point(interfaceOffset.Width + (idx - 1) * stepX, interfaceOffset.Height + stepY * (1.0 - (prevValue - minChannelValue) / height) + offsetY),
                            new Point(interfaceOffset.Width + idx * stepX, interfaceOffset.Height + stepY * (1.0 - (nowValue - minChannelValue) / height) + offsetY)
                        );
                    }

                    prevValue = nowValue;
                    idx++;
                }
            }
            #endregion [Chart] Draw

            #region [SelectInterval] Draw
            if (curSelected != -1)
            {
                double centerX = 0.0;
                if (optimization)
                {
                    centerX = interfaceOffset.Width + 2.0 * stepX * (curSelected - Begin) / stepOptimization;

                    dc.DrawLine(new Pen(Brushes.Green, 2.0),
                        new Point(centerX, interfaceOffset.Height),
                        new Point(centerX, actSize.Height + interfaceOffset.Height)
                    );
                }
                else
                {
                    centerX = interfaceOffset.Width + stepX * (curSelected - Begin);

                    dc.DrawLine(new Pen(Brushes.Green, 2.0),
                        new Point(centerX, interfaceOffset.Height),
                        new Point(centerX, actSize.Height + interfaceOffset.Height));
                }

                var formText1 = new FormattedText(this.MappingXAxis(curSelected, this),
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    12, Brushes.DarkGreen, VisualTreeHelper.GetDpi(this).PixelsPerDip
                );

                formText1.TextAlignment = TextAlignment.Center;

                dc.DrawRectangle(Brushes.LightGray, new Pen(Brushes.Black, 2.0), new Rect(
                    centerX - formText1.Width / 2, 0, formText1.Width, formText1.Height    
                ));
                dc.DrawText(formText1, new Point(centerX, 0.0));

                double centerY = actSize.Height / (maxChannelValue - minChannelValue) * (this.Channel.values[curSelected] - minChannelValue);
                centerY = actSize.Height - centerY;
                centerY += interfaceOffset.Height;

                dc.DrawLine(new Pen(Brushes.Green, 2.0),
                    new Point(interfaceOffset.Width, centerY),
                    new Point(interfaceOffset.Width + actSize.Width, centerY)
                );

                string val = Math.Round(this.Channel.values[curSelected], 5).ToString(CultureInfo.InvariantCulture);
                if (val.Length > this.MaxVAxisLength)
                {
                    val = val.Substring(0, this.MaxVAxisLength);
                }
                var formText2 = new FormattedText(val,
                    CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    12, Brushes.DarkGreen, VisualTreeHelper.GetDpi(this).PixelsPerDip
                );
                formText2.TextAlignment = TextAlignment.Right;

                dc.DrawRectangle(Brushes.LightGray, new Pen(Brushes.Black, 2.0), new Rect(
                    interfaceOffset.Width - formText2.Width, centerY - formText2.Height / 2, formText2.Width, formText2.Height
                ));
                dc.DrawText(formText2, new Point(interfaceOffset.Width, centerY - formText2.Height / 2));
            }

            if (enableSelectInterval)
            {
                var brush = new SolidColorBrush(Color.FromArgb(100, 255, 153, 51));
                if (optimization)
                {
                    dc.DrawRectangle(brush,
                        new Pen(Brushes.Transparent, 2.0),
                        new Rect(interfaceOffset.Width + 2.0 * stepX * (selectIntervalBegin - Begin) / stepOptimization,
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
                        new Rect(interfaceOffset.Width + stepX * (selectIntervalBegin - Begin),
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
            if (e.ChangedButton == MouseButton.Left && IsMouseSelect &&
                position.X >= interfaceOffset.Width &&
                position.Y >= interfaceOffset.Height)
            {
                int idx = GetIdx(position);
                selectIntervalBegin = selectIntervalEnd = Math.Clamp(idx, Begin, End);
                fakeBegin = selectIntervalBegin;

                enableSelectInterval = true;
                InvalidateVisual();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            var position = e.GetPosition(this);
            if (e.ChangedButton == MouseButton.Left && IsMouseSelect &&
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

                selectIntervalBegin = Math.Clamp(selectIntervalBegin, Begin, End);
                selectIntervalEnd = Math.Clamp(selectIntervalEnd, Begin, End);

                enableSelectInterval = false;

                if (selectIntervalEnd > selectIntervalBegin)
                {
                    Begin = selectIntervalBegin;
                    End = selectIntervalEnd;
                    OnMouseSelect(this, Begin, End);
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
                    curSelected = GetIdx(position);
                    //tooltip.IsOpen = true;
                    //tooltip.HorizontalOffset = 1;//position.X;
                    //tooltip.VerticalOffset = ActualHeight - 26;// position.Y - 20;
                    //tooltip.Content = $"X: {this.MappingXAxis(curSelected, this)}; Y: {Channel.values[curSelected]}";
                }

                if (enableSelectInterval && IsMouseSelect)
                {
                    int idx = GetIdx(position);

                    selectIntervalEnd = idx;
                    selectIntervalBegin = fakeBegin;
                    if (selectIntervalEnd < selectIntervalBegin)
                    {
                        selectIntervalBegin = idx;
                        selectIntervalEnd = fakeBegin;
                    }

                    selectIntervalBegin = Math.Clamp(selectIntervalBegin, Begin, End);
                    selectIntervalEnd = Math.Clamp(selectIntervalEnd, Begin, End);
                }

                InvalidateVisual();
            }
            else if (curSelected != -1)
            {
                //tooltip.IsOpen = false;
                curSelected = -1;
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (ShowCurrentXY)
            {
                if (curSelected != -1)
                {
                    //tooltip.IsOpen = false;
                    curSelected = -1;
                }
            }


            if (IsMouseSelect)
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
                idx = (int)Math.Round(position.X * stepOptimization / (2.0 * stepX) + Begin);
            }
            else
            {
                idx = (int)Math.Round(position.X / stepX + Begin);
            }
            return idx;
        }

        public IEnumerable<double> Optimize(int step)
        {
            if (step < 3)
            {
                for (int i = Begin; i <= End; i++)
                {
                    yield return Channel.values[i];
                }
                yield break;
            }

            double prevMaxValue = double.NaN;

            int iterations = (Length + step - 1) / step;
            for (int i = 0; i < iterations; i++)
            {
                double minValue = double.MaxValue;
                double maxValue = double.MinValue;
                for (int j = 0; j < step; j++)
                {
                    int idx = Begin + i * step + j;
                    if (idx > End) break;

                    minValue = Math.Min(minValue, Channel.values[idx]);
                    maxValue = Math.Max(maxValue, Channel.values[idx]);
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
