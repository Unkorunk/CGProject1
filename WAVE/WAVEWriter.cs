using System;
using System.IO;
using System.Text;

namespace FileFormats
{
    public class WAVEWriter : IWriter
    {
        public byte[] TryWrite(FileInfo fileInfo)
        {
            var stream = new MemoryStream();

            const int bytesPerSample = 4;

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
                            stream.Write(BitConverter.GetBytes((short)fileInfo.data[i, j]));
                            break;
                        case 4:
                            stream.Write(BitConverter.GetBytes((int)fileInfo.data[i, j]));
                            break;
                        case 8:
                            stream.Write(BitConverter.GetBytes((long)fileInfo.data[i, j]));
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
