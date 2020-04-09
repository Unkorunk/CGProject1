using System;
using System.Collections.Generic;
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
    }
}
