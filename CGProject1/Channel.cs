using System;
using System.Collections.Generic;
using System.Text;

namespace CGProject1 {
    public class Channel {
        public Channel(int samplesNum) {
            values = new double[samplesNum];
        }

        public string channelName;
        public double[] values;
    }
}
