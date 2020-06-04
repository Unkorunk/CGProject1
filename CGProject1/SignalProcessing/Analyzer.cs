using System;
using System.Collections.Generic;
using System.Numerics;


namespace CGProject1.SignalProcessing {
    public static class Analyzer {
        private static Channel curChannel;
        private static Complex[] ft;

        public static void SetupChannel(Channel channel) {
            curChannel = channel;
            ft = FastFourierTransform(channel);
            if (ft.Length >= 2) {
                ft[0] = ft[1];
            }
        }

        public static Channel AmplitudeSpectre(int halfWindowSmooth) {
            var res = new Channel(ft.Length / 2);
            res.Name = "Амп. спектр " + curChannel.Name;
            res.Source = "Analyzer";

            for (int i = 0; i < ft.Length / 2; i++) {
                res.values[i] = curChannel.DeltaTime * Complex.Abs(ft[i]);
            }

            if (halfWindowSmooth != 0 && res.values.Length > 0) {
                var q = new LinkedList<double>();
                double curWindow = 0;

                for (int i = 0; i < halfWindowSmooth * 2 + 1; i++) {
                    double curVal = res.values[(res.values.Length * halfWindowSmooth - halfWindowSmooth + i) % res.values.Length];
                    curWindow += curVal;
                    q.AddLast(curVal);
                }

                res.values[0] = curWindow / (2 * halfWindowSmooth + 1);

                for (int i = 1; i < res.values.Length; i++) {
                    curWindow -= q.First.Value;
                    q.RemoveFirst();
                    double curVal = res.values[(i + halfWindowSmooth) % res.values.Length];
                    curWindow += curVal;
                    q.AddLast(curVal);

                    res.values[i] = curWindow / (2 * halfWindowSmooth + 1);
                }
            }

            return res;
        }

        public static Channel PowerSpectralDensity(int halfWindowSmooth) {
            var res = new Channel(ft.Length / 2);
            res.Name = "СПМ " + curChannel.Name;
            res.Source = "Analyzer";

            var sqrDt = curChannel.DeltaTime * curChannel.DeltaTime;

            for (int i = 0; i < ft.Length / 2; i++) {
                res.values[i] = sqrDt * Math.Pow(Complex.Abs(ft[i]), 2);
            }

            return res;
        }

        private static Complex[] SlowFourierTransform(Channel channel) {
            int n = channel.SamplesCount;
            var res = new Complex[n];

            for (int i = 0; i < n; i++) {
                var val = Complex.Zero;

                for (int j = 0; j < n; j++) {
                    val += channel.DeltaTime * channel.values[j] * Complex.Exp(-Complex.ImaginaryOne * 2 * Math.PI * j * i / n);
                }

                res[i] = val;
            }

            return res;
        }

        private static Complex[] FastFourierTransform(Channel channel) {
            return SlowFourierTransform(channel);
        }

    }
}
