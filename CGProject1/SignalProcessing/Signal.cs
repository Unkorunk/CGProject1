using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CGProject1.SignalProcessing;

namespace CGProject1 {
    public class Signal {
        public string fileName;
        public ObservableCollection<Channel> channels;

        /// <summary>
        /// Sampling rate (frequency)
        /// </summary>
        public double SamplingFrq { get; set; }

        /// <summary>
        /// Amount of time between samples
        /// </summary>
        public double DeltaTime {
            get { return 1.0 / this.SamplingFrq; }
        }

        /// <summary>
        /// Number of samples in channels
        /// </summary>
        public int SamplesCount {
            get {
                if (channels.Count == 0) {
                    return 0;
                }

                return channels[0].values.Length;
            }
        }

        /// <summary>
        /// Overall duration of signal
        /// </summary>
        public TimeSpan Duration {
            get { return TimeSpan.FromSeconds(DeltaTime * this.SamplesCount); }
        }

        /// <summary>
        /// Time of record's start
        /// </summary>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Time of record's end
        /// </summary>
        public DateTime EndTime {
            get { return StartDateTime.Add(Duration); }
        }

        public Signal() {
            this.fileName = "НовыйФайл.txt";
            this.StartDateTime = SignalProcessing.Modelling.defaultStartDateTime;
            this.channels = new ObservableCollection<Channel>();
        }

        public Signal(string name) {
            this.fileName = name;
            this.StartDateTime = SignalProcessing.Modelling.defaultStartDateTime;
            this.channels = new ObservableCollection<Channel>();
        }

        public void UpdateChannelsInfo() {
            foreach (var channel in channels) {
                channel.StartDateTime = this.StartDateTime;
                channel.SamplingFrq = this.SamplingFrq;
            }
        }

        public DateTime GetDateTimeAtIndex(int index)
        {
            if (index < 0 || index >= this.SamplesCount) throw new ArgumentException();
            return StartDateTime + TimeSpan.FromSeconds(DeltaTime * index);
        }

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append($"{{Name: {fileName}, ");
            sb.Append($"Cnt: {SamplesCount}, ");
            sb.Append($"Channels: [");
            foreach (var channel in channels) {
                sb.Append($"{channel.Name} from {channel.Source}, ");
            }
            sb.Append("]}");
            return sb.ToString();
        }
    }
}
