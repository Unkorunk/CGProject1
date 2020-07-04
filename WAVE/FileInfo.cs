using System;

namespace FileFormats
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
