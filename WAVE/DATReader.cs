using System;
using System.Globalization;
using System.Text;

namespace FileFormats
{
    public class DATReader : IReader
    {
        public bool TryRead(byte[] data, out FileInfo fileInfo)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var windows1251 = Encoding.GetEncoding("windows-1251");

            fileInfo = new FileInfo();

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

            string begtime = windows1251.GetString(data, offset, 19);
            offset += 30;
            //begtime = begtime.TrimEnd('\0');

            // unused
            offset += 30;

            // reserved
            offset += 8;

            // deprecated
            offset += 2;

            // reserved
            offset += 44;

            if (data.Length < 128 + n_points * n_chan * 4) return false;

            fileInfo.nChannels = n_chan;
            fileInfo.nSamplesPerSec = 1e6 / timecad;

            if (!DateTime.TryParseExact(begtime, "MM-dd-yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out fileInfo.dateTime))
            {
                return false;
            }

            fileInfo.data = new double[n_points, n_chan];
            for (int i = 0; i < n_points; i++)
            {
                for (int j = 0; j < n_chan; j++)
                {
                    if (offset >= data.Length) return false;
                    fileInfo.data[i, j] = BitConverter.ToSingle(data, offset);
                    offset += 4;
                }
            }

            // TODO: specify format description

            // Footer processing
            if (offset < data.Length - 1) {
                int extdMark = (int)BitConverter.ToSingle(data, offset);
                offset += 4;

                //if (extdMark == 64747865) {
                int milliseconds = (int)BitConverter.ToSingle(data, offset);
                offset += 4;
                fileInfo.dateTime = fileInfo.dateTime.AddMilliseconds(milliseconds);

                int chanNameLength = (int)BitConverter.ToSingle(data, offset);
                offset += 4;

                // useless (chanNameOffset == sizeof(footer))
                offset += 4;

                int unitNameLength = (int)BitConverter.ToSingle(data, offset);
                offset += 4;

                // useless (unitNameOffset == sizeof(footer) + chanNameLength)
                offset += 4;

                if (chanNameLength == 0) {
                    var rawChannelNames = windows1251.GetString(data, offset, data.Length - offset).Split(';');
                    if (rawChannelNames.Length >= n_chan) {
                        fileInfo.channelNames = rawChannelNames[0..n_chan];
                    } else {
                        fileInfo.channelNames = new string[n_chan];
                    }
                } else {
                    var rawChannelNames = windows1251.GetString(data, offset, chanNameLength).Split(';');
                    offset += chanNameLength;

                    if (rawChannelNames.Length == n_chan) {
                        fileInfo.channelNames = rawChannelNames[0..n_chan];
                    } else {
                        fileInfo.channelNames = new string[n_chan];
                    }

                    string unitName = windows1251.GetString(data, offset, unitNameLength);
                }

                    
                //} else {
                //    fileInfo.channelNames = new string[n_chan];
                //}

            } else {
                fileInfo.channelNames = new string[n_chan];
            }

            return true;
        }
    }
}
