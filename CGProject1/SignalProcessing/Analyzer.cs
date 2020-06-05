using System;
using System.Collections.Generic;
using System.Numerics;


namespace CGProject1.SignalProcessing {
    public class Analyzer {
        private Channel curChannel;
        private Complex[] ft;

        private int bound = 4096;

        public int HalfWindowSmoothing { get; set; }

        private double[] amps = null;
        private double[] psds = null;

        public enum ZeroMode {
            Nothing,
            Null,
            Smooth
        }

        public ZeroMode zeroMode = ZeroMode.Nothing;

        public Analyzer(Channel channel) {
            this.curChannel = channel;
        }

        public void SetupChannel(int begin, int end) {
            if (end - begin < bound) {
                ft = SlowFourierTransform(curChannel, begin, end);
            } else {
                int len = end - begin + 1;
                int closestPowerOfTwo = (int)Math.Pow(2, (int)Math.Log2(len));
                begin += (len - closestPowerOfTwo) / 2;
                end = begin + closestPowerOfTwo - 1;

                ft = FastFourierTransform(curChannel, begin, end);
            }
            
            if (ft.Length >= 2) {
                ft[0] = ft[1];
            }

            amps = new double[ft.Length];

            for (int i = 0; i < ft.Length; i++) {
                amps[i] = curChannel.DeltaTime * Complex.Abs(ft[i]);
            }

            var sqrDt = curChannel.DeltaTime * curChannel.DeltaTime;
            psds = new double[ft.Length];

            for (int i = 0; i < ft.Length; i++) {
                psds[i] = sqrDt * Math.Pow(Complex.Abs(ft[i]), 2);
            }
        }

        /// <summary>
        /// DEPRECATED! Debug only
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        public void SetupSlowChannel(Channel channel, int begin, int end) {
            curChannel = channel;

            ft = SlowFourierTransform(channel, begin, end);

            if (ft.Length >= 2) {
                ft[0] = ft[1];
            }
        }

        public Channel LogarithmicSpectre() {
            int n = ft.Length / 2;
            var res = new Channel(n);
            res.Name = "Лог. Спектр " + curChannel.Name;
            res.Source = "Analyzer";

            for (int i = 0; i < n; i++) {
                res.values[i] = 20 * Math.Log10(amps[i]);
            }

            switch (this.zeroMode) {
                case ZeroMode.Smooth:
                    if (res.values.Length > 1) {
                        res.values[0] = res.values[1];
                    }
                    break;
                case ZeroMode.Null:
                    res.values[0] = 0;
                    break;
                default:
                    break;
            }

            WindowSmoothing(res, HalfWindowSmoothing);

            res.SamplingFrq = 2.0 / (curChannel.SamplingFrq / res.SamplesCount);

            return res;
        }

        public Channel AmplitudeSpectre() {
            int n = ft.Length / 2;
            var res = new Channel(n);
            res.Name = "Спектр " + curChannel.Name;
            res.Source = "Analyzer";

            for (int i = 0; i < n; i++) {
                res.values[i] = amps[i];
            }

            switch (this.zeroMode) {
                case ZeroMode.Smooth:
                    if (res.values.Length > 1) {
                        res.values[0] = res.values[1];
                    }
                    break;
                case ZeroMode.Null:
                    res.values[0] = 0;
                    break;
                default:
                    break;
            }

            WindowSmoothing(res, HalfWindowSmoothing);

            //var newDx = 1.0 / (2 * curChannel.DeltaTime * res.SamplesCount);
            //res.SamplingFrq = 1.0 / newDx;

            res.SamplingFrq = 2.0 / (curChannel.SamplingFrq / res.SamplesCount);

            return res;
        }

        public Channel PowerSpectralDensity() {
            int n = ft.Length / 2;
            var res = new Channel(n);
            res.Name = "Спектр " + curChannel.Name;
            res.Source = "Analyzer";

            for (int i = 0; i < n; i++) {
                res.values[i] = psds[i]; ;
            }

            switch (this.zeroMode) {
                case ZeroMode.Smooth:
                    if (res.values.Length > 1) {
                        res.values[0] = res.values[1];
                    }
                    break;
                case ZeroMode.Null:
                    res.values[0] = 0;
                    break;
                default:
                    break;
            }

            WindowSmoothing(res, HalfWindowSmoothing);

            //var newDx = 1.0 / (2 * curChannel.DeltaTime * res.SamplesCount);
            //res.SamplingFrq = 1.0 / newDx;

            res.SamplingFrq = 2.0 / (curChannel.SamplingFrq / res.SamplesCount);

            return res;
        }

        private void WindowSmoothing(Channel channel, int halfWindow) {
            if (halfWindow != 0 && channel.values.Length > 0) {
                var q = new LinkedList<double>();
                double curWindow = 0;

                for (int i = 0; i < halfWindow * 2 + 1; i++) {
                    double curVal = channel.values[(channel.values.Length * halfWindow - halfWindow + i) % channel.values.Length];
                    curWindow += curVal;
                    q.AddLast(curVal);
                }

                channel.values[0] = curWindow / (2 * halfWindow + 1);

                for (int i = 1; i < channel.values.Length; i++) {
                    curWindow -= q.First.Value;
                    q.RemoveFirst();
                    double curVal = channel.values[(i + halfWindow) % channel.values.Length];
                    curWindow += curVal;
                    q.AddLast(curVal);

                    channel.values[i] = curWindow / (2 * halfWindow + 1);
                }
            }
        }

        private Complex[] SlowFourierTransform(Channel channel, int begin, int end) {
            int n = end - begin + 1;
            var res = new Complex[n];

            for (int i = 0; i < n; i++) {
                var val = Complex.Zero;

                for (int j = 0; j < n; j++) {
                    val += channel.values[begin + j] * Complex.Exp(-Complex.ImaginaryOne * 2 * Math.PI * j * i / n);
                }

                res[i] = val;
            }

            for (int i = 0; i < n; i++) {
                res[i] *= channel.DeltaTime;
            }

            return res;
        }

        private Complex[] FastFourierTransform(Channel channel, int begin, int end) {
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

        private void InnerFastFourierTransform(ref Complex[] a) {
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
