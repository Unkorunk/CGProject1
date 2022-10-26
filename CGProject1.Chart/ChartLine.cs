using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CGProject1.SignalProcessing;

namespace CGProject1.Chart
{
    public class ChartLine : FrameworkElement
    {
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
        
        public bool Selected { get; set; }

        public Segment Segment { get; }

        public bool GridDraw { get; set; }

        public bool ShowCurrentXY { get; set; }

        private Size interfaceOffset = new Size();
        private bool optimization = false;
        private double stepX = 1.0;
        private int stepOptimization = 0;

        //private ToolTip tooltip;

        private int curSelected = -1;

        public ChartLine(Channel channel)
        {
            Channel = channel;

            Segment = new Segment(0, Math.Max(0, channel.SamplesCount - 1));
            Segment.OnChange += (sender, change) => InvalidateVisual();
            
            _groupedCharts = new List<ChartLine>() { this };

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

            if (Segment.Length < 2) return;

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

            Size actSize = new Size(Math.Max(0, ActualWidth - interfaceOffset.Width),
                Math.Max(0 ,ActualHeight - interfaceOffset.Height));

            if ((DisplayHAxisTitle || DisplayHAxisInfo) && HAxisAlligment == HAxisAlligmentEnum.Bottom)
            {
                interfaceOffset.Height = 0;
            }
            #endregion [Interface] Reserve

            #region [Optimization]
            const double startOptimizationWith = 1.0;

            stepX = actSize.Width / (Segment.Length - 1);
            optimization = stepX < startOptimizationWith;
            stepOptimization = 0;
            if (optimization)
            {
                stepOptimization = (int)Math.Ceiling(startOptimizationWith * Segment.Length / actSize.Width);
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

                        for (int i = Segment.Left; i <= Segment.Right; i++)
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

                        for (int i = Segment.Left; i <= Segment.Right; i++)
                        {
                            minChannelValue = Math.Min(minChannelValue, Channel.values[i]);
                            maxChannelValue = Math.Max(maxChannelValue, Channel.values[i]);
                        }

                        foreach (var chart in GroupedCharts)
                        {
                            for (int i = chart.Segment.Left; i <= chart.Segment.Right; i++)
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

                        for (int i = Segment.Left; i <= Segment.Right; i++)
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
                double curX = Segment.Left;
                double len = Segment.Right - Segment.Left;
                double xPart = len / 10;

                double dx = Math.Pow(10, Math.Ceiling(Math.Log10(xPart)));
                if (xPart < dx / 2) {
                    dx /= 2;
                    if (xPart < dx / 2) {
                        dx /= 2;
                    }
                }

                curX = Math.Ceiling(curX / dx) * dx;

                var ht = 0.0;
                while (curX <= Segment.Right) {
                    var x = (curX - Segment.Left) / len * actSize.Width;
                    dc.DrawLine(new Pen(Brushes.Gray, 1.0), new Point(interfaceOffset.Width + x, interfaceOffset.Height),
                            new Point(interfaceOffset.Width + x, interfaceOffset.Height + actSize.Height));

                    if (DisplayHAxisInfo) {
                        string XAxisText = MappingXAxis((int)curX, this);

                        var formText1 = new FormattedText(XAxisText,
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Times New Roman"),
                            12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                        );
                        formText1.TextAlignment = TextAlignment.Center;

                        if (HAxisAlligment == HAxisAlligmentEnum.Bottom) {
                            var offsetY1 = actSize.Height;
                            ht = Math.Max(ht, offsetY1 + formText1.Height);
                            dc.DrawText(formText1, new Point(interfaceOffset.Width + x, offsetY1));
                        } else {
                            dc.DrawText(formText1, new Point(interfaceOffset.Width + x, interfaceOffset.Height - (formText1.Height + 1)));
                        }
                    }

                    curX += dx;
                }

                if (DisplayHAxisTitle) {
                    var formText3 = new FormattedText(HAxisTitle,
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Times New Roman"),
                        12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                    );

                    formText3.TextAlignment = TextAlignment.Center;

                    dc.DrawText(formText3, new Point(interfaceOffset.Width + actSize.Width / 2, ht));
                }

                double yLen = maxChannelValue - minChannelValue;
                double yPart = yLen / 10;
                double dy = Math.Pow(10, Math.Ceiling(Math.Log10(yPart)));

                if (yPart < dy / 2) {
                    dy /= 2;
                    if (yPart < dy / 2) {
                        dy /= 2;
                    }
                }

                double curY = Math.Ceiling(minChannelValue / dy) * dy;

                while (curY <= maxChannelValue) {
                    var y = actSize.Height - (curY - minChannelValue) / yLen * actSize.Height;

                    dc.DrawLine(new Pen(Brushes.Gray, 1.0), new Point(interfaceOffset.Width, interfaceOffset.Height + y),
                        new Point(interfaceOffset.Width + actSize.Width, interfaceOffset.Height + y));

                    string val = Math.Round(curY, 5).ToString(CultureInfo.InvariantCulture);
                    if (val.Length > this.MaxVAxisLength) {
                        val = val.Substring(0, this.MaxVAxisLength);
                    }
                    if (DisplayVAxisInfo) {
                        var formText1 = new FormattedText(val,
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Times New Roman"),
                            12, Brushes.Blue, VisualTreeHelper.GetDpi(this).PixelsPerDip
                        );

                        formText1.TextAlignment = TextAlignment.Right;

                        dc.DrawText(formText1, new Point(interfaceOffset.Width, interfaceOffset.Height + y - formText1.Height / 2));
                    }

                    curY += dy;
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
                    centerX = interfaceOffset.Width + 2.0 * stepX * (curSelected - Segment.Left) / stepOptimization;

                    dc.DrawLine(new Pen(Brushes.Green, 2.0),
                        new Point(centerX, interfaceOffset.Height),
                        new Point(centerX, actSize.Height + interfaceOffset.Height)
                    );
                }
                else
                {
                    centerX = interfaceOffset.Width + stepX * (curSelected - Segment.Left);

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

                if (centerY + formText2.Height / 2 > Height) {
                    centerY -= centerY + formText2.Height / 2 - Height + 1;
                }

                if (centerY - formText2.Height / 2 < 0) {
                    centerY -= centerY - formText2.Height / 2;
                }

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
                        new Rect(interfaceOffset.Width + 2.0 * stepX * (selectIntervalBegin - Segment.Left) / stepOptimization,
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
                        new Rect(interfaceOffset.Width + stepX * (selectIntervalBegin - Segment.Left),
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
                selectIntervalBegin = selectIntervalEnd = Math.Clamp(idx, Segment.Left, Segment.Right);
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

                selectIntervalBegin = Math.Clamp(selectIntervalBegin, Segment.Left, Segment.Right);
                selectIntervalEnd = Math.Clamp(selectIntervalEnd, Segment.Left, Segment.Right);

                enableSelectInterval = false;

                if (selectIntervalEnd > selectIntervalBegin)
                {
                    Segment.SetLeftRight(selectIntervalBegin, selectIntervalEnd);
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
                    if (curSelected >= this.Channel.SamplesCount) {
                        curSelected = this.Channel.SamplesCount - 1;
                    }
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

                    selectIntervalBegin = Math.Clamp(selectIntervalBegin, Segment.Left, Segment.Right);
                    selectIntervalEnd = Math.Clamp(selectIntervalEnd, Segment.Left, Segment.Right);
                }

                InvalidateVisual();
            }
            else if (curSelected != -1)
            {
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
                idx = (int)Math.Round(position.X * stepOptimization / (2.0 * stepX) + Segment.Left);
            }
            else
            {
                idx = (int)Math.Round(position.X / stepX + Segment.Left);
            }
            return idx;
        }

        public IEnumerable<double> Optimize(int step)
        {
            if (step < 3)
            {
                for (int i = Segment.Left; i <= Segment.Right; i++)
                {
                    yield return Channel.values[i];
                }
                yield break;
            }

            double prevMaxValue = double.NaN;

            int iterations = (Segment.Length + step - 1) / step;
            for (int i = 0; i < iterations; i++)
            {
                double minValue = double.MaxValue;
                double maxValue = double.MinValue;
                for (int j = 0; j < step; j++)
                {
                    int idx = Segment.Left + i * step + j;
                    if (idx > Segment.Right) break;

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
