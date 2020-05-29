using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace CGProject1
{
    public class Histogram : FrameworkElement
    {
        private List<double> _data;
        public List<double> Data {
            get => _data;
            set {
                _data = value;
                InvalidateVisual();
            }
        }

        public Histogram()
        {
            Data = new List<double>();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (Data.Count == 0) return;

            double stepX = ActualWidth / Data.Count;
            var darkGrayPen = new Pen(Brushes.DarkGray, 1.0);
            var maxValue = Data.Max();
            for (int i = 0; i < Data.Count; i++)
            {
                double height = ActualHeight * Data[i] / maxValue;
                dc.DrawRectangle(Brushes.Gray, darkGrayPen, new Rect(i * stepX, ActualHeight - height, stepX, height));
            }
        }
    }
}
