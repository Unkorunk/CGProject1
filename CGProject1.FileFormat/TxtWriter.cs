using System.Globalization;
using System.IO;
using System.Text;

namespace FileFormats
{
    public class TxtWriter : IWriter
    {
        public byte[] TryWrite(FileInfo fileInfo)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var stream = new MemoryStream();

            using (var sw = new StreamWriter(stream, Encoding.GetEncoding("windows-1251")))
            {
                sw.WriteLine("# channels number");
                sw.WriteLine(fileInfo.nChannels);

                sw.WriteLine("# samples number");
                sw.WriteLine(fileInfo.data.GetLength(0));

                sw.WriteLine("# sampling rate");
                sw.WriteLine(fileInfo.nSamplesPerSec.ToString(CultureInfo.InvariantCulture));

                sw.WriteLine("# start date");
                sw.WriteLine(fileInfo.dateTime.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));

                sw.WriteLine("# start time");
                sw.WriteLine(fileInfo.dateTime.ToString("HH\\:mm\\:ss\\.fff", CultureInfo.InvariantCulture));

                sw.WriteLine("# channels names");
                sw.WriteLine(string.Join(';', fileInfo.channelNames));

                for (int i = 0; i < fileInfo.data.GetLength(0); i++)
                {
                    var vals = "";
                    for (int j = 0; j < fileInfo.data.GetLength(1); j++)
                    {
                        vals += fileInfo.data[i, j].ToString(CultureInfo.InvariantCulture) + " ";
                    }

                    sw.WriteLine(vals);
                }
            }

            return stream.ToArray();
        }
    }
}
