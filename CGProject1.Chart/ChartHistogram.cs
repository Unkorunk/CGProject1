using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace CGProject1.Chart
{
    public class ChartHistogram : FrameworkElement
    {
        public double[] Data { get; set; }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (Data == null || Data.Length == 0) return;

            double stepX = ActualWidth / Data.Length;
            var darkGrayPen = new Pen(Brushes.DarkGray, 1.0);
            var maxValue = Data.Max();
            for (int i = 0; i < Data.Length; i++)
            {
                double height = ActualHeight * Data[i] / maxValue;
                dc.DrawRectangle(Brushes.Gray, darkGrayPen, new Rect(i * stepX, ActualHeight - height, stepX, height));
            }
        }
    }
}
