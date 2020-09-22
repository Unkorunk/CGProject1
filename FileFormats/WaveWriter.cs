using System;
using System.IO;
using System.Text;

namespace FileFormats
{
    public class WaveWriter : IWriter
    {
        public byte[] TryWrite(FileInfo fileInfo)
        {
            var stream = new MemoryStream();

            const int bytesPerSample = 4;
            for (int i = 0; i < fileInfo.nChannels; i++)
            {
                double minValue = double.MaxValue;
                double maxValue = double.MinValue;

                for (int j = 0; j < fileInfo.data.GetLength(0); j++)
                {
                    minValue = Math.Min(minValue, fileInfo.data[j, i]);
                    maxValue = Math.Max(maxValue, fileInfo.data[j, i]);
                }

                for (int j = 0; j < fileInfo.data.GetLength(0); j++)
                {
                    fileInfo.data[j, i] = 2.0 * (fileInfo.data[j, i] - minValue) / (maxValue - minValue) - 1.0;
                }
            }

            // ckID
            stream.Write(Encoding.ASCII.GetBytes("RIFF"));
            // cksize
            int cksize = 4 + 24 + 8 + bytesPerSample * fileInfo.nChannels * fileInfo.data.GetLength(0) + fileInfo.data.GetLength(0) % 2;
            stream.Write(BitConverter.GetBytes(cksize));
            // WAVEID
            stream.Write(Encoding.ASCII.GetBytes("WAVE"));
            // ckID
            stream.Write(Encoding.ASCII.GetBytes("fmt "));
            // cksize
            stream.Write(BitConverter.GetBytes(16));
            // wFormatTag
            stream.Write(BitConverter.GetBytes((short)0x0001));
            // nChannels
            stream.Write(BitConverter.GetBytes((short)fileInfo.nChannels));
            // nSamplesPerSec
            int blocksPerSecond = (int)Math.Ceiling(fileInfo.nSamplesPerSec);
            stream.Write(BitConverter.GetBytes(blocksPerSecond));
            // nAvgBytesPerSec
            stream.Write(BitConverter.GetBytes(blocksPerSecond * fileInfo.nChannels * bytesPerSample));
            // nBlockAlign
            stream.Write(BitConverter.GetBytes((short)(fileInfo.nChannels * bytesPerSample)));
            // wBitsPerSample
            stream.Write(BitConverter.GetBytes((short)(8 * bytesPerSample)));
            // ckID
            stream.Write(Encoding.ASCII.GetBytes("data"));
            // cksize
            stream.Write(BitConverter.GetBytes(bytesPerSample * fileInfo.nChannels * fileInfo.data.GetLength(0)));
            // sampled data
            for (int i = 0; i < fileInfo.data.GetLength(0); i++)
            {
                for (int j = 0; j < fileInfo.data.GetLength(1); j++)
                {
                    switch (bytesPerSample)
                    {
                        case 2:
                            short curVal1;
                            if (fileInfo.data[i, j] > 0)
                            {
                                curVal1 = (short)(short.MaxValue * fileInfo.data[i, j]);
                            }
                            else
                            {
                                curVal1 = (short)(short.MinValue * -fileInfo.data[i, j]);
                            }

                            stream.Write(BitConverter.GetBytes(curVal1));
                            break;
                        case 4:
                            int curVal2;
                            if (fileInfo.data[i, j] > 0)
                            {
                                curVal2 = (int)(int.MaxValue * fileInfo.data[i, j]);
                            }
                            else
                            {
                                curVal2 = (int)(int.MinValue * -fileInfo.data[i, j]);
                            }

                            stream.Write(BitConverter.GetBytes(curVal2));
                            break;
                        case 8:
                            long curVal3;
                            if (fileInfo.data[i, j] > 0)
                            {
                                curVal3 = (long)(long.MaxValue * fileInfo.data[i, j]);
                            }
                            else
                            {
                                curVal3 = (long)(long.MinValue * -fileInfo.data[i, j]);
                            }

                            stream.Write(BitConverter.GetBytes(curVal3));
                            break;
                        default:
                            return null;
                    }
                }
            }

            if (fileInfo.data.GetLength(0) % 2 == 1)
            {
                stream.Write(new byte[1]);
            }

            return stream.ToArray();
        }
    }
}
