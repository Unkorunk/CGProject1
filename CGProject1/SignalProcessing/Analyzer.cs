using System;
using System.Numerics;
using System.Collections.Generic;
using MathNet.Numerics.IntegralTransforms;

namespace CGProject1.SignalProcessing
{
    public class Analyzer
    {
        private readonly Channel myChannel;

        public int HalfWindowSmoothing { get; set; }

        private double[] asd;
        private double[] psd;

        public ZeroModeEnum ZeroMode { get; set; } = ZeroModeEnum.Nothing;

        public int SamplesCount { get; private set; }

        public Analyzer(Channel channel)
        {
            myChannel = channel;
        }

        public Analyzer(double[] vals, double frq)
        {
            myChannel = new Channel(vals.Length);
            myChannel.SamplingFrq = frq;

            for (int i = 0; i < vals.Length; i++)
            {
                myChannel.values[i] = vals[i];
            }
        }

        public void SetupChannel(int begin, int end)
        {
            int len = end - begin;
            var values = new Complex[len];
            for (int i = 0; i < len; i++)
            {
                values[i] = myChannel.values[i + begin];
            }

            Fourier.Forward(values, FourierOptions.NoScaling);
            var ft = values;

            SamplesCount = ft.Length / 2;

            asd = new double[ft.Length];
            for (int i = 0; i < ft.Length; i++)
            {
                asd[i] = myChannel.DeltaTime * Complex.Abs(ft[i]);
            }

            psd = new double[ft.Length];
            for (int i = 0; i < ft.Length; i++)
            {
                psd[i] = Math.Pow(asd[i], 2);
            }
        }

        public Channel LogarithmicAsd()
        {
            var res = new Channel(SamplesCount) {Name = $"[Logarithmic ASD] {myChannel.Name}", Source = "Analyzer"};

            for (int i = 0; i < SamplesCount; i++)
            {
                res.values[i] = 20 * Math.Log10(asd[i]);
            }

            FinalSetup(res);

            return res;
        }

        public Channel LogarithmicPsd()
        {
            var res = new Channel(SamplesCount) {Name = $"[Logarithmic PSD] {myChannel.Name}", Source = "Analyzer"};

            for (int i = 0; i < SamplesCount; i++)
            {
                res.values[i] = 10 * Math.Log10(psd[i]);
            }

            FinalSetup(res);

            return res;
        }

        public Channel AmplitudeSpectralDensity()
        {
            var res = new Channel(SamplesCount) {Name = $"[ASD] {myChannel.Name}", Source = "Analyzer"};

            for (int i = 0; i < SamplesCount; i++)
            {
                res.values[i] = asd[i];
            }

            FinalSetup(res);

            return res;
        }

        public Channel PowerSpectralDensity()
        {
            var res = new Channel(SamplesCount) {Name = $"[PSD] {myChannel.Name}", Source = "Analyzer"};

            for (int i = 0; i < SamplesCount; i++)
            {
                res.values[i] = psd[i];
            }

            FinalSetup(res);

            return res;
        }

        private void FinalSetup(Channel channel)
        {
            switch (ZeroMode)
            {
                case ZeroModeEnum.Smooth:
                    if (channel.values.Length > 1)
                    {
                        channel.values[0] = channel.values[1];
                    }

                    break;
                case ZeroModeEnum.Null:
                    if (channel.values.Length != 0)
                    {
                        channel.values[0] = 0;
                    }

                    break;
                case ZeroModeEnum.Nothing:
                    break;
                default:
                    throw new NotImplementedException();
            }

            WindowSmoothing(channel, HalfWindowSmoothing);

            channel.SamplingFrq = 2.0 / (myChannel.SamplingFrq / channel.SamplesCount);
        }

        private void WindowSmoothing(Channel channel, int halfWindow)
        {
            if (halfWindow != 0 && channel.values.Length > 0)
            {
                var q = new LinkedList<double>();
                double curWindow = 0;

                for (int i = 0; i < halfWindow * 2 + 1; i++)
                {
                    int curIdx = Math.Abs(0 - halfWindow + i);
                    if (curIdx >= channel.values.Length)
                    {
                        curIdx -= (curIdx % channel.values.Length) + 1;
                    }

                    double curVal = channel.values[curIdx];
                    curWindow += curVal;
                    q.AddLast(curVal);
                }

                channel.values[0] = curWindow / (2 * halfWindow + 1);

                for (int i = 1; i < channel.values.Length; i++)
                {
                    curWindow -= q.First.Value;
                    q.RemoveFirst();

                    int curIdx = Math.Abs(i + halfWindow);
                    if (curIdx >= channel.values.Length)
                    {
                        curIdx -= (curIdx % channel.values.Length) + 1;
                    }

                    double curVal = channel.values[curIdx];
                    curWindow += curVal;
                    q.AddLast(curVal);

                    channel.values[i] = curWindow / (2 * halfWindow + 1);
                }
            }
        }
    }
}