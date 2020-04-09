using System;
using System.Collections.Generic;
using System.Text;

namespace CGProject1 {
    public class Signal {
        public Signal() {
            this.fileName = "НовыйФайл.txt";
            this.startDateTime = DateTime.Now;
            this.samplingFrq = 1;
        }

        public Signal(string name) {
            this.fileName = name;
        }

        public string fileName;
        public DateTime startDateTime;
        public double samplingFrq;
        public Channel[] channels;

        public double DeltaTime {
            get {
                return 1.0 / this.samplingFrq;
            }
        }

        public int SamplesCount {
            get {
                if (channels.Length == 0) {
                    return 0;
                }

                return channels[0].values.Length;
            }
        }

        public TimeSpan Duration {
            get {
                return TimeSpan.FromSeconds(DeltaTime * this.SamplesCount);
            }
        }

        public DateTime EndTime {
            get {
                return startDateTime.Add(Duration);
            }
        }

    }
}
