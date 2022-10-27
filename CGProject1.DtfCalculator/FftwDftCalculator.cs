using System.Numerics;
using CGProject1.SignalProcessing;
using FFTWSharp;

namespace CGProject1.DtfCalculator
{
    public class FftwDftCalculator : IDftCalculator
    {
        public Complex[] Calculate(Complex[] input)
        {
            var arr = new fftwf_complexarray(input);
            var outArr = new fftwf_complexarray(input.Length);

            var plan = fftwf_plan.dft_1d(input.Length, arr, outArr, fftw_direction.Forward, fftw_flags.Estimate);
            plan.Execute();

            return outArr.GetData_Complex();
        }
    }
}