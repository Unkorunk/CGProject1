﻿using System;
using System.Collections.Generic;
using System.Numerics;

using FFTWSharp;


namespace CGProject1.SignalProcessing {
    public class Analyzer {
        private Channel curChannel;
        private Complex[] ft;

        public int HalfWindowSmoothing { get; set; }

        private double[] amps = null;
        private double[] psds = null;

        public enum ZeroMode {
            Nothing,
            Null,
            Smooth
        }

        public ZeroMode zeroMode = ZeroMode.Nothing;

        public int SamplesCount { get {
                if (ft == null) {
                    return 0; 
                }
                return ft.Length / 2; 
            } 
        }

        public string OriginalName { get => this.curChannel.Name; }

        public Analyzer(Channel channel) {
            this.curChannel = channel;
        }

        public Analyzer(double[] vals, double frq) {
            curChannel = new Channel(vals.Length);
            curChannel.SamplingFrq = frq;

            for (int i = 0; i < vals.Length; i++) {
                curChannel.values[i] = vals[i];
            }
        }

        public void SetupChannel(int begin, int end, bool forceFast = false, bool expand = false) {
            int len = end - begin;
            Complex[] vals = new Complex[len];
            for (int i = 0; i < len; i++) {
                vals[i] = curChannel.values[i + begin];
            }

            //Fourier.Forward(vals, FourierOptions.NoScaling);
            //ft = vals;
            ft = FFT(vals);

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

        public Channel LogarithmicPSD() {
            int n = ft.Length / 2;
            var res = new Channel(n);
            res.Name = "Лог. Спектр " + curChannel.Name;
            res.Source = "Analyzer";

            for (int i = 0; i < n; i++) {
                res.values[i] = 10 * Math.Log10(psds[i]);
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
                    int curIdx = Math.Abs(0 - halfWindow + i);
                    if (curIdx >= channel.values.Length) {
                        curIdx -= (curIdx % channel.values.Length) + 1;
                    }
                    double curVal = channel.values[curIdx];
                    curWindow += curVal;
                    q.AddLast(curVal);
                }

                channel.values[0] = curWindow / (2 * halfWindow + 1);

                for (int i = 1; i < channel.values.Length; i++) {
                    curWindow -= q.First.Value;
                    q.RemoveFirst();

                    int curIdx = Math.Abs(i + halfWindow);
                    if (curIdx >= channel.values.Length) {
                        curIdx -= (curIdx % channel.values.Length) + 1;
                    }

                    double curVal = channel.values[curIdx];
                    curWindow += curVal;
                    q.AddLast(curVal);

                    channel.values[i] = curWindow / (2 * halfWindow + 1);
                }
            }
        }

        private Complex[] FFT(Complex[] input) {
            var arr = new fftwf_complexarray(input);
            var outArr = new fftwf_complexarray(input.Length);

            var plan = fftwf_plan.dft_1d(input.Length, arr, outArr, fftw_direction.Forward, fftw_flags.Estimate);
            plan.Execute();

            return outArr.GetData_Complex();
        }
    }
}
