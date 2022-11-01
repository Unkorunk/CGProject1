namespace CGProject1.FileFormat.API
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
