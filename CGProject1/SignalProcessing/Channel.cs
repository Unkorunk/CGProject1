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

        public double SamplingFrq { get; set; }

        public double DeltaTime {
            get { return 1.0 / this.SamplingFrq; }
        }

        public int SamplesCount {
            get {
                return this.values.Length;
            }
        }

        public TimeSpan Duration {
            get { return TimeSpan.FromSeconds(DeltaTime * this.SamplesCount); }
        }

        public DateTime StartDateTime { get; set; }

        public DateTime EndTime {
            get { return StartDateTime.Add(Duration); }
        }
    }
}
