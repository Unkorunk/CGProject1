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
    }
}
