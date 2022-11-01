using System;
using System.IO;
using CGProject1.FileFormat.API;
using NAudio.Wave;
using FileInfo = CGProject1.FileFormat.API.FileInfo;

namespace CGProject1.FileFormat
{
    public class Mp3Reader : IReader
    {
        public bool IsMp3FileReader { get; set; } = true;

        public bool TryRead(Stream stream, out FileInfo fileInfo)
        {
            fileInfo = new FileInfo();

            using WaveStream file = IsMp3FileReader
                ? new Mp3FileReader(stream)
                : new WaveFileReader(stream);

            fileInfo.nChannels = file.WaveFormat.Channels;
            fileInfo.nSamplesPerSec = file.WaveFormat.SampleRate;
            fileInfo.channelNames = new string[fileInfo.nChannels];

            if (file.WaveFormat.BitsPerSample % 8 != 0) return false;

            var bytesPerSample = file.WaveFormat.BitsPerSample / 8;
            var samples = file.Length / (bytesPerSample * fileInfo.nChannels);

            fileInfo.data = new double[samples, fileInfo.nChannels];

            var bytes = new byte[file.Length];
            var n = file.Read(bytes, 0, (int)file.Length);
            for (var i = 0; i < n; i += bytesPerSample * fileInfo.nChannels)
            {
                for (var j = 0; j < fileInfo.nChannels; j++)
                {
                    var startIndex = i + j * bytesPerSample;
                    var sample = i / (bytesPerSample * fileInfo.nChannels);
                    switch (bytesPerSample)
                    {
                        case 2:
                            fileInfo.data[sample, j] = BitConverter.ToInt16(bytes, startIndex);
                            break;
                        case 4:
                            fileInfo.data[sample, j] = BitConverter.ToInt32(bytes, startIndex);
                            break;
                        case 8:
                            fileInfo.data[sample, j] = BitConverter.ToInt64(bytes, startIndex);
                            break;
                        default:
                            return false;
                    }
                }
            }

            return true;
        }
    }
}