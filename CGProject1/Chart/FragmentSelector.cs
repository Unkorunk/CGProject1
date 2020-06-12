using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CGProject1.Chart
{
    public class FragmentSelector : FrameworkElement
    {
        public event EventHandler IntervalUpdate;

        private int _maximum = 1;
        public int Maximum { get => _maximum; set { _maximum = value; InvalidateVisual(); } }
        private int _minimum = 0;
        public int Minimum { get => _minimum; set { _minimum = value; InvalidateVisual(); } }

        private bool _leftSliderSelected = false;
        private int _leftSlider = 0;
        public int LeftSlider
        {
            get => _leftSlider;
            set
            {
                _leftSlider = Math.Clamp(value, Minimum, RightSlider - 1);
                IntervalUpdate?.Invoke(this, null);
                InvalidateVisual();
            }
        }

        private bool _rightSliderSelected = false;
        private int _rightSlider = 1;
        public int RightSlider
        {
            get => _rightSlider;
            set
            {
                _rightSlider = Math.Clamp(value, LeftSlider + 1, Maximum);
                IntervalUpdate?.Invoke(this, null);
                InvalidateVisual();
            }
        }

        private bool _intervalSelected = false;
        private int _intervalCenter = 0;

        private double _padding = 0.1;
        public double Padding
        {
            get => _padding;
            set
            {
                _padding = Math.Clamp(value, 0.0, 1.0);
                InvalidateVisual();
            }
        }

        private double _sliderWidth = 10.0;
        private double SliderWidth { get => _sliderWidth; set { _sliderWidth = value; InvalidateVisual(); } }

        protected override void OnRender(DrawingContext drawingContext)
        {
            double visualPadding = ActualHeight * Padding;

            drawingContext.DrawRectangle(Brushes.Gray, new Pen(Brushes.Black, 1.0), new Rect(
                SliderWidth,
                visualPadding,
                this.ActualWidth - 2 * SliderWidth,
                this.ActualHeight - 2 * visualPadding
            ));

            double visualLength = (ActualWidth - 2 * SliderWidth) / (Maximum - Minimum);

            double leftSliderX = visualLength * (LeftSlider - Minimum);
            double rightSliderX = SliderWidth + visualLength * (RightSlider - Minimum);

            drawingContext.DrawRectangle(_leftSliderSelected ? Brushes.LightBlue : Brushes.AliceBlue, new Pen(Brushes.Black, 1.0), new Rect(
                leftSliderX, 0, SliderWidth, ActualHeight
            ));
            drawingContext.DrawRectangle(_rightSliderSelected ? Brushes.LightBlue : Brushes.AliceBlue, new Pen(Brushes.Black, 1.0), new Rect(
                rightSliderX, 0, SliderWidth, ActualHeight
            ));
            if (rightSliderX - leftSliderX - SliderWidth - 2 > 0)
            {
                drawingContext.DrawRectangle(_intervalSelected ? Brushes.DarkOrange : Brushes.Orange, new Pen(Brushes.Transparent, 1.0), new Rect(
                    leftSliderX + SliderWidth + 1, visualPadding + 1, rightSliderX - leftSliderX - SliderWidth - 2, this.ActualHeight - 2 * visualPadding - 2
                ));
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            double visualPadding = ActualHeight * Padding;

            double visualLength = (ActualWidth - 2 * SliderWidth) / (Maximum - Minimum);

            double leftSliderX = visualLength * (LeftSlider - Minimum);
            double rightSliderX = SliderWidth + visualLength * (RightSlider - Minimum);

            Rect leftSlider = new Rect(leftSliderX, 0, SliderWidth, ActualHeight);
            Rect rightSlider = new Rect(rightSliderX, 0, SliderWidth, ActualHeight);

            Point point = e.GetPosition(this);
            if (leftSlider.Contains(point))
            {
                _leftSliderSelected = true;
            }
            else if (rightSlider.Contains(point))
            {
                _rightSliderSelected = true;
            }
            else if (rightSliderX - leftSliderX - SliderWidth - 2 > 0)
            {
                Rect interval = new Rect(
                    leftSliderX + SliderWidth + 1, visualPadding + 1, rightSliderX - leftSliderX - SliderWidth - 2, this.ActualHeight - 2 * visualPadding - 2
                );
                if (interval.Contains(point))
                {
                    int sliderVal = (int)Math.Round(point.X / visualLength + Minimum);
                    _intervalSelected = true;
                    _intervalCenter = sliderVal - LeftSlider;
                }
            }

            InvalidateVisual();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            double visualLength = (ActualWidth - 2 * SliderWidth) / (Maximum - Minimum);
            Point point = e.GetPosition(this);

            if (_leftSliderSelected)
            {
                int sliderVal = (int)Math.Round((point.X - SliderWidth / 2) / visualLength + Minimum);
                LeftSlider = sliderVal;
            }
            else if (_rightSliderSelected)
            {
                int sliderVal = (int)Math.Round((point.X - 3 * SliderWidth / 2) / visualLength + Minimum);
                RightSlider = sliderVal;
            }
            else if (_intervalSelected)
            {
                int sliderVal = (int)Math.Round(point.X / visualLength + Minimum);
                int offset = (sliderVal - LeftSlider) - (_intervalCenter);

                int newLeft = Math.Clamp(LeftSlider + offset, Minimum, RightSlider - 1);
                int noffset = newLeft - LeftSlider;
                int newRight = Math.Clamp(RightSlider + noffset, newLeft + 1, Maximum);

                if (newRight - newLeft == RightSlider - LeftSlider)
                {
                    _leftSlider = newLeft;
                    _rightSlider = newRight;
                    IntervalUpdate?.Invoke(this, null);
                }
            }

            InvalidateVisual();
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            _leftSliderSelected = false;
            _rightSliderSelected = false;
            _intervalSelected = false;

            InvalidateVisual();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            _leftSliderSelected = false;
            _rightSliderSelected = false;
            _intervalSelected = false;

            InvalidateVisual();
        }
    }
}
