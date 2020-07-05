using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CGProject1 {
    public class Channel : INotifyPropertyChanged {
        public Channel(int samplesNum) {
            values = new double[samplesNum];
        }

        public string Name { get; set; }
        public string Source { get; set; }

        public double[] values;

        public event PropertyChangedEventHandler PropertyChanged;

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
