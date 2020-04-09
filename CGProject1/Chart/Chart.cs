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
            double stepY = this.ActualHeight;

            double offsetY = this.ActualHeight / 2.0 - stepY / 2.0;

            double channelMinValue = this.channel.MinValue;
            double channelMaxValue = this.channel.MaxValue;

            double height = Math.Abs(channelMaxValue - channelMinValue);

            dc.DrawRectangle(this.Selected ? Brushes.LightBlue : Brushes.LightGray,
                new Pen(Brushes.DarkGray, 2.0), new Rect(0, 0, ActualWidth, ActualHeight));
            for (int i = this.Begin + 1; i <= this.End; i++)
            {
                dc.DrawLine(
                    new Pen(Brushes.Black, 1.0),
                    new Point((i - this.Begin - 1) * stepX, stepY * (this.channel.values[i - 1] - channelMinValue) / height + offsetY),
                    new Point((i - this.Begin) * stepX, stepY * (this.channel.values[i] - channelMinValue) / height + offsetY)
                );
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
