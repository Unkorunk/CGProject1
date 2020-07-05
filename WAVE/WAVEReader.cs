using System;

namespace FileFormats
{
    public class WAVEReader : IReader
    {
        private const ushort WAVE_FORMAT_PCM = 0x0001;

        private struct FmtInfo
        {
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
        }

        private struct HeaderInfo
        {
            public string ckID;
            public int cksize;
        }

        public bool TryRead(byte[] data, out FileInfo fileInfo)
        {
            fileInfo = new FileInfo();

            int offset = 0;

            if (data.Length < 12) return false;

            var headerInfo = GetHeaderInfo(data, ref offset);
            if (headerInfo.ckID != "RIFF")
            {
                return false;
            }

            int expectedOffset1 = offset + headerInfo.cksize;

            string WAVEID = System.Text.Encoding.ASCII.GetString(data, offset, 4);
            if (WAVEID != "WAVE") return false;
            offset += 4;

            FmtInfo fmtInfo = new FmtInfo();
            bool fmtInfoSet = false;

            while (offset < expectedOffset1)
            {
                var headerInfo1 = GetHeaderInfo(data, ref offset);
                if (headerInfo1.cksize < 0 || offset + headerInfo1.cksize > expectedOffset1) {
                    return false;
                }

                if (headerInfo1.ckID == "fmt ")
                {
                    if (headerInfo1.cksize != 16) return false;

                    if (!fmtChunk(data, ref offset, ref fileInfo, out fmtInfo))
                    {
                        return false;
                    }

                    fmtInfoSet = true;
                }
                else if (headerInfo1.ckID == "data")
                {
                    if (fmtInfoSet && dataChunk(data, ref offset, ref fileInfo, fmtInfo, headerInfo1))
                    {
                        return true;
                    }

                    return false;
                }
                else
                {
                    offset += headerInfo1.cksize;
                }
            }

            return false;
        }

        private static HeaderInfo GetHeaderInfo(byte[] data, ref int offset)
        {
            var headerInfo = new HeaderInfo();

            headerInfo.ckID = System.Text.Encoding.ASCII.GetString(data, offset, 4);
            offset += 4;

            headerInfo.cksize = BitConverter.ToInt32(data, offset);
            offset += 4;

            return headerInfo;
        }

        private static bool fmtChunk(byte[] data, ref int offset, ref FileInfo waveFile, out FmtInfo fmtInfo)
        {
            fmtInfo = new FmtInfo();

            ushort wFormatTag = BitConverter.ToUInt16(data, offset);
            if (wFormatTag != WAVE_FORMAT_PCM) return false;
            offset += 2;

            waveFile.nChannels = BitConverter.ToUInt16(data, offset);
            offset += 2;

            waveFile.nSamplesPerSec = BitConverter.ToInt32(data, offset);
            offset += 4;

            fmtInfo.nAvgBytesPerSec = BitConverter.ToUInt32(data, offset);
            offset += 4;

            fmtInfo.nBlockAlign = BitConverter.ToUInt16(data, offset);
            offset += 2;

            fmtInfo.wBitsPerSample = BitConverter.ToUInt16(data, offset);
            offset += 2;

            waveFile.channelNames = new string[waveFile.nChannels];

            return true;
        }

        private static bool dataChunk(byte[] data, ref int offset, ref FileInfo waveFile, in FmtInfo fmtInfo, in HeaderInfo headerInfo)
        {
            int bytesPerSample = fmtInfo.nBlockAlign / waveFile.nChannels;
            int bytesPerBlock = bytesPerSample * waveFile.nChannels;
            int totalBlocks = headerInfo.cksize / bytesPerBlock;
            waveFile.data = new double[totalBlocks, waveFile.nChannels];

            for (int blockIdx = 0; blockIdx < totalBlocks; blockIdx++)
            {
                for (int channelIdx = 0; channelIdx < waveFile.nChannels; channelIdx++)
                {
                    switch(bytesPerSample)
                    {
                        case 2:
                            waveFile.data[blockIdx, channelIdx] = BitConverter.ToInt16(data, offset + channelIdx * bytesPerSample);
                            break;
                        case 4:
                            waveFile.data[blockIdx, channelIdx] = BitConverter.ToInt32(data, offset + channelIdx * bytesPerSample);
                            break;
                        case 8:
                            waveFile.data[blockIdx, channelIdx] = BitConverter.ToInt64(data, offset + channelIdx * bytesPerSample);
                            break;
                        default:
                            return false;
                    }
                }

                offset += bytesPerBlock;
            }

            if (headerInfo.cksize % 2 == 1) offset++;

            return true;
        }
    }
}
