using System;

namespace CGProject1
{
    public class Segment
    {
        [Flags]
        public enum SegmentChange
        {
            None = 0,
            MinValue = 1,
            MaxValue = 2,
            Left = 4,
            Right = 8
        }
        
        public delegate void DelChange(Segment sender, SegmentChange segmentChange);

        public DelChange OnChange;

        private int _minValue = 0;
        private int _maxValue = 0;
        private int _left = 0;
        private int _right = 0;

        public int MinValue
        {
            get => _minValue;
            set
            {
                var oldMinValue = _minValue;
                _minValue = Math.Min(value, _maxValue);
                if (oldMinValue != _minValue) OnChange?.Invoke(this, SegmentChange.MinValue);
            }
        }

        public int MaxValue
        {
            get => _maxValue;
            set
            {
                var oldMaxValue = _maxValue;
                _maxValue = Math.Max(value, _minValue);
                if (oldMaxValue != _maxValue) OnChange?.Invoke(this, SegmentChange.MaxValue);
            }
        }

        public int Left
        {
            get => _left;
            set
            {
                var oldLeft = _left;
                _left = Math.Clamp(value, MinValue, Right);
                if (oldLeft != _left) OnChange?.Invoke(this, SegmentChange.Left);
            }
        }

        public int Right
        {
            get => _right;
            set
            {
                var oldRight = _right;
                _right = Math.Clamp(value, Left, MaxValue);
                if (oldRight != _right) OnChange?.Invoke(this, SegmentChange.Right);
            }
        }

        public int Length => Right - Left + 1;

        public Segment()
        {
            _minValue = 0;
            _maxValue = 0;
            _left = 0;
            _right = 0;
        }
        
        public Segment(int minValue, int maxValue)
        {
            if (minValue > maxValue) throw new ArgumentException();
            
            _minValue = minValue;
            _maxValue = maxValue;

            _left = _minValue;
            _right = _maxValue;
        }

        public Segment(int left, int right, int minValue, int maxValue) : this(minValue, maxValue)
        {
            if (left > right) throw new ArgumentException();
            
            _left = Math.Clamp(left, _minValue, _right);
            _right = Math.Clamp(right, _left, _maxValue);
        }

        public void SetLeftRight(int left, int right)
        {
            if (left > right) throw new ArgumentException();

            var segmentChange = SegmentChange.None;
            int oldLeft = _left, oldRight = _right;

            _left = Math.Clamp(left, _minValue, _maxValue);
            _right = Math.Clamp(right, _minValue, _maxValue);

            if (oldLeft != _left) segmentChange |= SegmentChange.Left;
            if (oldRight != _right) segmentChange |= SegmentChange.Right;
            
            if (segmentChange != SegmentChange.None) OnChange?.Invoke(this, segmentChange);
        }

        public void SetMinMax(int minValue, int maxValue)
        {
            if (minValue > maxValue) throw new ArgumentException();

            var segmentChange = SegmentChange.None;
            int oldLeft = _left, oldRight = _right;
            int oldMinValue = _minValue, oldMaxValue = _maxValue;
            
            _minValue = minValue;
            _maxValue = maxValue;
            _left = Math.Clamp(_left, _minValue, _maxValue);
            _right = Math.Clamp(_right, _minValue, _maxValue);

            if (oldMinValue != _minValue) segmentChange |= SegmentChange.MinValue;
            if (oldMaxValue != _maxValue) segmentChange |= SegmentChange.Right;
            if (oldLeft != _left) segmentChange |= SegmentChange.Left;
            if (oldRight != _right) segmentChange |= SegmentChange.Right;
            
            if (segmentChange != SegmentChange.None) OnChange?.Invoke(this, segmentChange);
        }

        public void SetSegment(Segment segment)
        {
            _left = segment._left;
            _right = segment._right;
            _minValue = segment._minValue;
            _maxValue = segment._maxValue;

            OnChange?.Invoke(this,
                SegmentChange.MinValue | SegmentChange.Right | SegmentChange.Left | SegmentChange.Right);
        }

        public void SetLeftRight(Segment segment)
        {
            if (_minValue != segment._minValue ||
                _maxValue != segment._maxValue) throw new ArgumentException();
            SetLeftRight(segment._left, segment._right);
        }
    }
}
