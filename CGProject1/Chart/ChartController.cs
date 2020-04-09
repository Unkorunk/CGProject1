using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CGProject1.Chart
{
    class ChartController
    {
        private Channel channel;
        private Canvas canvas;

        private int begin = 0;
        private int end = 0;
        public int Begin { get => begin; set => begin = Math.Max(0, Math.Min(value, this.channel.values.Length - 1)); }
        public int End { get => end; set => end = Math.Max(0, Math.Min(value, this.channel.values.Length - 1)); }

        public int Length { get => End - Begin + 1; }

        public ChartController(in Canvas canvas, in Channel channel)
        {
            this.channel = channel;
            this.canvas = canvas;
            this.canvas.Loaded += (object sender, System.Windows.RoutedEventArgs e) => Redraw();
        }

        public void Redraw()
        {
            double stepX = this.canvas.Width / this.Length;
            double stepY = 30.0;
           
            double offsetY = this.canvas.Height / 2.0 - stepY / 2.0;

            double channelMinValue = this.channel.MinValue;
            double channelMaxValue = this.channel.MaxValue;

            double height = Math.Abs(channelMaxValue - channelMinValue);

            this.canvas.Children.Clear();
            for (int i = this.Begin + 1; i <= this.End; i++)
            {
                var line = new Line
                {
                    X1 = (i - this.Begin - 1) * stepX,
                    Y1 = stepY * (this.channel.values[i - 1] - channelMinValue) / height + offsetY,
                    X2 = (i - this.Begin) * stepX,
                    Y2 = stepY * (this.channel.values[i] - channelMinValue) / height + offsetY,

                    Stroke = Brushes.Black,
                    StrokeThickness = 1.0
                };

                this.canvas.Children.Add(line);
            }


        }
    }
}
