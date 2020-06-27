using System;
using System.Collections.Generic;
using System.Text;

namespace WAVE
{
    public class WaveFile
    {
        public int nChannels;
        public int nSamplesPerSec;

        public double[,] data;
    }
}
