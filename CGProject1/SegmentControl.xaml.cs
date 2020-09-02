using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CGProject1
{
    public partial class SegmentControl : UserControl
    {
        private Func<int, string> myRightFilter, myLeftFilter;
        private readonly Segment mySegment;
        private int mySmallStep = 10;
        private int myBigStep = 40;
        
        public SegmentControl(Segment segment)
        {
            InitializeComponent();
            MySegmentSelector.Segment.OnChange += OnChange;

            mySegment = segment;
            mySegment.OnChange += MySegment_OnChange;
            MySegmentSelector.Segment.SetSegment(mySegment);

            mySmallStep = (segment.Right - segment.Left) / 20;
            myBigStep = (segment.Right - segment.Left) / 5;
        }

        private void MySegment_OnChange(Segment sender, Segment.SegmentChange changes)
        {
            MySegmentSelector.Segment.SetSegment(sender);
            mySmallStep = (sender.Right - sender.Left) / 20;
            myBigStep = (sender.Right - sender.Left) / 5;
        }

        private void OnChange(Segment sender, Segment.SegmentChange changes)
        {
            var isLeft = changes.HasFlag(Segment.SegmentChange.Left);
            var isRight = changes.HasFlag(Segment.SegmentChange.Right);

            if (isLeft && isRight)
            {
                mySegment.SetLeftRight(sender);
            }
            else if (isRight && mySegment.Right != sender.Right)
            {
                mySegment.Right = sender.Right;
            }
            else if (isLeft && mySegment.Left != sender.Left)
            {
                mySegment.Left = sender.Left;
            }
            
            if (isRight)
            {
                if (myRightFilter != null)
                {
                    RightLabel.Content = myRightFilter(sender.Right);
                }

                var rightText = sender.Right.ToString();
                if (RightTextBox.Text != rightText)
                    RightTextBox.Text = rightText;
                
                LengthTextBox.Text = sender.Length.ToString();
            }
            
            if (isLeft)
            {
                if (myLeftFilter != null)
                {
                    LeftLabel.Content = myLeftFilter(sender.Left);
                }

                var leftText = sender.Left.ToString();
                if (LeftTextBox.Text != leftText)
                    LeftTextBox.Text = leftText;
                
                LengthTextBox.Text = sender.Length.ToString();
            }
        }

        public void SetLeftFilter(Func<int, string> leftFilter) => myLeftFilter = leftFilter;
        public void SetRightFilter(Func<int, string> rightFilter) => myRightFilter = rightFilter;

        private void RightTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(RightTextBox.Text, out var end))
            {
                end = int.MaxValue;
            }

            if (MySegmentSelector.Segment.Right != end)
                MySegmentSelector.Segment.Right = end;
        }

        private void LeftTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(LeftTextBox.Text, out var begin))
            {
                begin = int.MinValue;
            }

            if (MySegmentSelector.Segment.Left != begin)
                MySegmentSelector.Segment.Left = begin;
        }

        private void RightArrow_Click(object sender, RoutedEventArgs e) {
            MySegmentSelector.Segment.Shift(mySmallStep);
        }

        private void LeftArrow_Click(object sender, RoutedEventArgs e) {
            MySegmentSelector.Segment.Shift(-mySmallStep);
        }

        private void DoubleLeftArrow_Click(object sender, RoutedEventArgs e) {
            MySegmentSelector.Segment.Shift(-myBigStep);
        }

        private void DoubleRightArrow_Click(object sender, RoutedEventArgs e) {
            MySegmentSelector.Segment.Shift(myBigStep);
        }

        #region Validators
        private void PreviewTextInputHandler(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !TextIsNumeric(e.Text);
        }

        private void PreviewPastingHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var input = (string) e.DataObject.GetData(typeof(string));
                if (!TextIsNumeric(input)) e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static bool TextIsNumeric(string input)
        {
            return input.All(c => char.IsDigit(c) || char.IsControl(c));
        }
        #endregion Validators
    }
}
