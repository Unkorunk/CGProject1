using System;
using System.Collections.Generic;
using System.Numerics;


namespace CGProject1.SignalProcessing {
    public static class Analyzer {
        private static Channel curChannel;
        private static Complex[] ft;

        private static int bound = 4096;

        public static void SetupChannel(Channel channel, int begin, int end) {
            curChannel = channel;
            if (end - begin < bound) {
                ft = SlowFourierTransform(channel, begin, end);
            } else {
                int len = end - begin + 1;
                int closestPowerOfTwo = (int)Math.Pow(2, (int)Math.Log2(len));
                begin += (len - closestPowerOfTwo) / 2;
                end = begin + closestPowerOfTwo - 1;

                ft = FastFourierTransform(channel, begin, end);
            }
            
            if (ft.Length >= 2) {
                ft[0] = ft[1];
            }
        }

        /// <summary>
        /// DEPRECATED! Debug only
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public static void SetupSlowChannel(Channel channel, int begin, int end) {
            curChannel = channel;

                ft = SlowFourierTransform(channel, begin, end);


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

        private static Complex[] SlowFourierTransform(Channel channel, int begin, int end) {
            int n = end - begin + 1;
            var res = new Complex[n];

            for (int i = 0; i < n; i++) {
                var val = Complex.Zero;

                for (int j = 0; j < n; j++) {
                    val += channel.DeltaTime * channel.values[begin + j] * Complex.Exp(-Complex.ImaginaryOne * 2 * Math.PI * j * i / n);
                }

                res[i] = val;
            }

            return res;
        }

        private static Complex[] FastFourierTransform(Channel channel, int begin, int end) {
            int n = end - begin + 1;

            var res = new Complex[n];

            for (int i = 0; i < n; i++) {
                res[i] = channel.values[begin + i];
            }

            InnerFastFourierTransform(ref res);

            for (int i = 0; i < n; i++) {
                res[i] *= channel.DeltaTime;
            }

            return res;
        }

        private static void InnerFastFourierTransform(ref Complex[] a) {
            int n = a.Length;
            if (n == 1) {
                return;
            }

            double ang = -2 * Math.PI / n;

            var w = Complex.One;
            var wn = new Complex(Math.Cos(ang), Math.Sin(ang));

            var a0 = new Complex[n / 2];
            var a1 = new Complex[n / 2];

            for (int i = 0; i * 2 < n; i++) {
                a0[i] = a[2 * i];
                a1[i] = a[2 * i + 1];
            }

            InnerFastFourierTransform(ref a0);
            InnerFastFourierTransform(ref a1);

            for (int i = 0; i < n / 2; i++) {
                a[i] = a0[i] + w * a1[i];
                a[i + n / 2] = a0[i] - w * a1[i];
                w *= wn;
            }
        }

        //private static void InnerSlowFourierTransform(ref Complex[] a) {
        //    int n = a.Length;
        //    var res = new Complex[n];

        //    for (int i = 0; i < n; i++) {
        //        var val = Complex.Zero;

        //        for (int j = 0; j < n; j++) {
        //            val += a[j] * Complex.Exp(-Complex.ImaginaryOne * 2 * Math.PI * j * i / n);
        //        }

        //        res[i] = val;
        //    }

        //    for (int i = 0; i < n; i++) {
        //        a[i] = res[i];
        //    }

        //    return;
        //}
    }
}
