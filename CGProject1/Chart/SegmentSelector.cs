using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace CGProject1.Chart
{
    public class SegmentSelector : Thumb
    {
        public Segment Segment { get; } = new Segment();

        private bool leftSliderSelected;
        private bool rightSliderSelected;
        private bool intervalSelected;

        private double intervalCenter;
        
        private Rect leftSliderRect;
        private Rect rightSliderRect;
        private Rect intervalRect;

        private const double padding = 0.15;
        private const double sliderWidth = 10.0;
        private readonly Pen sliderPen = new Pen(Brushes.Black, 1.0);
        private readonly Pen intervalPen = new Pen(Brushes.Transparent, 1.0);

        public SegmentSelector()
        {
            Template = null;
            this.Segment.OnChange += (sender, change) =>
            {
                if (change != Segment.SegmentChange.None)
                {
                    InvalidateVisual();
                }
            };
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var maximum = Segment.MaxValue;
            var minimum = Segment.MinValue;
            var leftSlider = Segment.Left;
            var rightSlider = Segment.Right;

            var visualPadding = ActualHeight * padding;

            drawingContext.DrawRectangle(Brushes.Gray, new Pen(Brushes.Black, 1.0), new Rect(
                sliderWidth,
                visualPadding,
                Math.Max(0, ActualWidth - 2 * sliderWidth),
                Math.Max(0, ActualHeight - 2 * visualPadding)
            ));

            var visualLength = (ActualWidth - 2 * sliderWidth) / (maximum - minimum);

            var leftSliderX = visualLength * (leftSlider - minimum);
            var rightSliderX = sliderWidth + visualLength * (rightSlider - minimum);

            leftSliderRect = new Rect(leftSliderX, 0, sliderWidth, ActualHeight);
            rightSliderRect = new Rect(rightSliderX, 0, sliderWidth, ActualHeight);

            drawingContext.DrawRectangle(leftSliderSelected ? Brushes.LightBlue : Brushes.AliceBlue, sliderPen, leftSliderRect);
            drawingContext.DrawRectangle(rightSliderSelected ? Brushes.LightBlue : Brushes.AliceBlue, sliderPen, rightSliderRect);
            
            if (rightSliderX - leftSliderX - sliderWidth - 2 > 0)
            {
                intervalRect = new Rect(
                    leftSliderX + sliderWidth + 1,
                    visualPadding + 1,
                    rightSliderX - leftSliderX - sliderWidth - 2,
                    ActualHeight - 2 * visualPadding - 2
                );

                drawingContext.DrawRectangle(intervalSelected ? Brushes.DarkOrange : Brushes.Orange, intervalPen, intervalRect);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            var maximum = Segment.MaxValue;
            var minimum = Segment.MinValue;
            var leftSlider = Segment.Left;
            var rightSlider = Segment.Right;

            var visualLength = (ActualWidth - 2 * sliderWidth) / (maximum - minimum);

            var leftSliderX = visualLength * (leftSlider - minimum);
            var rightSliderX = sliderWidth + visualLength * (rightSlider - minimum);

            var mousePosition = e.GetPosition(this);
            if (leftSliderRect.Contains(mousePosition))
            {
                leftSliderSelected = true;
            }
            else if (rightSliderRect.Contains(mousePosition))
            {
                rightSliderSelected = true;
            }
            else if (rightSliderX - leftSliderX - sliderWidth - 2 > 0)
            {
                if (intervalRect.Contains(mousePosition))
                {
                    intervalSelected = true;
                    intervalCenter = mousePosition.X - (sliderWidth + leftSliderX);
                }
            }

            InvalidateVisual();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var maximum = Segment.MaxValue;
            var minimum = Segment.MinValue;

            var visualLength = (ActualWidth - 2 * sliderWidth) / (maximum - minimum);
            var mousePosition = e.GetPosition(this);

            if (leftSliderSelected)
            {
                var sliderVal = (int) Math.Round((mousePosition.X - sliderWidth / 2) / visualLength + minimum);
                Segment.Left = sliderVal;
            }
            else if (rightSliderSelected)
            {
                var sliderVal = (int) Math.Round((mousePosition.X - 3 * sliderWidth / 2) / visualLength + minimum);
                Segment.Right = sliderVal;
            }
            else if (intervalSelected)
            {
                var leftX = Math.Max(mousePosition.X - sliderWidth - intervalCenter, 0.0);
                var leftVal = PositionToValue(leftX);
                var rightVal = leftVal + Segment.Length - 1;
                
                if (rightVal < Segment.MaxValue) Segment.SetLeftRight(leftVal, rightVal);
            }

            InvalidateVisual();
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            leftSliderSelected = false;
            rightSliderSelected = false;
            intervalSelected = false;

            InvalidateVisual();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            leftSliderSelected = false;
            rightSliderSelected = false;
            intervalSelected = false;

            InvalidateVisual();
        }

        private double GetVisualLength()
        {
            return (ActualWidth - 2 * sliderWidth) / (Segment.MaxValue - Segment.MinValue);
        }

        private double ValueToPosition(int val)
        {
            if (val > Segment.MaxValue || val < Segment.MinValue) throw new ArgumentException();
            return sliderWidth + GetVisualLength() * (val - Segment.MinValue);
        }

        private int PositionToValue(double position)
        {
            if (position < 0.0 || position > ActualWidth - 2 * sliderWidth) throw new ArgumentException();
            return (int) Math.Round(position / GetVisualLength());
        }
    }
}
