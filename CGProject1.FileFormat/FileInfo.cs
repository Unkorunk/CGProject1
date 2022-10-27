using System;

namespace CGProject1.FileFormat
{
    public class FileInfo
    {
        public int nChannels;
        public double nSamplesPerSec;

        public DateTime dateTime;

        public double[,] data;
        public string[] channelNames;
    }
}
