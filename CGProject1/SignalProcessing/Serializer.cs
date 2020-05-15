using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace CGProject1.SignalProcessing {
    public class Serializer {
        public static void Serialize(string path, Signal signal, int begin, int end) {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var sw = new StreamWriter(path, false, Encoding.GetEncoding("windows-1251"))) {
                sw.WriteLine("# channels number");
                sw.WriteLine(signal.channels.Count);

                sw.WriteLine("# samples number");
                sw.WriteLine(end - begin + 1);

                sw.WriteLine("# sampling rate");
                sw.WriteLine(signal.SamplingFrq.ToString(CultureInfo.InvariantCulture));

                sw.WriteLine("# start date");
                var curStart = signal.StartDateTime + TimeSpan.FromSeconds(begin * signal.DeltaTime);
                sw.WriteLine(curStart.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));

                sw.WriteLine("# start time");
                sw.WriteLine(curStart.ToString("HH\\:mm\\:ss\\.fff", CultureInfo.InvariantCulture));

                sw.WriteLine("# channels names");
                string names = "";
                foreach (var channel in signal.channels) {
                    names += channel.Name + ";";
                }
                sw.WriteLine(names);

                for (int i = begin; i <= end; i++) {
                    var vals = "";
                    foreach (var channel in signal.channels) {
                        vals += channel.values[i].ToString(CultureInfo.InvariantCulture) + " ";
                    }

                    sw.WriteLine(vals);
                }
            }
        }
    }
}
