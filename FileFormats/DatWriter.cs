using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace FileFormats
{
    public class DatWriter : IWriter
    {
        public byte[] TryWrite(FileInfo fileInfo)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var windows1251 = Encoding.GetEncoding("windows-1251");

            var stream = new MemoryStream();

            stream.Write(BitConverter.GetBytes(0.0f));
            stream.Write(BitConverter.GetBytes((float)fileInfo.data.GetLength(0)));
            stream.Write(BitConverter.GetBytes((short)fileInfo.nChannels));
            stream.Write(BitConverter.GetBytes((float)(1e6 / fileInfo.nSamplesPerSec)));
            
            var begtimeBytes = windows1251.GetBytes(fileInfo.dateTime.ToString("MM-dd-yyyy HH:mm:ss", CultureInfo.InvariantCulture));
            Array.Resize(ref begtimeBytes, 30);
            stream.Write(begtimeBytes);

            var endtime = fileInfo.dateTime.AddSeconds(fileInfo.data.GetLength(0) / fileInfo.nSamplesPerSec);
            var endtimeBytes = windows1251.GetBytes(endtime.ToString("MM-dd-yyyy HH:mm:ss", CultureInfo.InvariantCulture));
            Array.Resize(ref endtimeBytes, 30);
            stream.Write(endtimeBytes);

            stream.Write(BitConverter.GetBytes(0.0f));
            stream.Write(BitConverter.GetBytes(0.0f));
            stream.Write(BitConverter.GetBytes((short)1));
            stream.Write(new byte[44]);
            for (int i = 0; i < fileInfo.data.GetLength(0); i++)
            {
                for (int j = 0; j < fileInfo.data.GetLength(1); j++)
                {
                    stream.Write(BitConverter.GetBytes((float)fileInfo.data[i, j]));
                }
            }

            // TODO: channel names

            return stream.ToArray();
        }
    }
}
