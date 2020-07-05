using System;
using System.Globalization;
using System.Text;

namespace FileFormats
{
    public class DatReader : IReader
    {
        public bool TryRead(byte[] data, out FileInfo waveFile)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var windows1251 = Encoding.GetEncoding("windows-1251");

            waveFile = new FileInfo();

            if (data.Length < 128) return false;

            int offset = 0;

            // unused
            offset += 4;

            int n_points = (int)BitConverter.ToSingle(data, offset);
            offset += 4;

            short n_chan = BitConverter.ToInt16(data, offset);
            offset += 2;

            float timecad = BitConverter.ToSingle(data, offset);
            offset += 4;

            string begtime = windows1251.GetString(data, offset, 30);
            offset += 30;
            begtime = begtime.TrimEnd('\0');

            // unused
            offset += 30;

            // reserved
            offset += 8;

            // deprecated
            offset += 2;

            // reserved
            offset += 44;

            if (data.Length < 128 + n_points * n_chan * 4) return false;

            waveFile.nChannels = n_chan;
            waveFile.nSamplesPerSec = 1e6 / timecad;

            waveFile.dateTime = DateTime.ParseExact(begtime, "MM-dd-yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            waveFile.data = new double[n_points, n_chan];
            for (int i = 0; i < n_points; i++)
            {
                for (int j = 0; j < n_chan; j++)
                {
                    if (offset >= data.Length) return false;
                    waveFile.data[i, j] = BitConverter.ToSingle(data, offset);
                    offset += 4;
                }
            }

            // TODO: specify format description

            var rawChannelNames = windows1251.GetString(data, offset, data.Length - offset).Split(';');
            if (rawChannelNames.Length != 0)
            {
                if (Math.Abs(rawChannelNames.Length - n_chan) > 1) return false;
                waveFile.channelNames = rawChannelNames[0..n_chan];
            } else
            {
                waveFile.channelNames = new string[n_chan];
            }

            return true;
        }
    }
}
