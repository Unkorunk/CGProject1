using System.Numerics;

namespace CGProject1.SignalProcessing
{
    public interface IDftCalculator
    {
        Complex[] Calculate(Complex[] input);
    }
}