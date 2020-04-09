using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGProject1 {
    public class Channel {
        public Channel(int samplesNum, string source) {
            values = new double[samplesNum];
            Source = source;
        }

        public string Name { get; set; }
        public string Source { get; set; }

        public double[] values;

        public double MaxValue { get => values.Max(); }
        public double MinValue { get => values.Min(); }
    }
}
