using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace FileFormats
{
    public class TxtReader : IReader
    {
        private enum ParseState
        {
            NeedChannelNumber,
            NeedSamplesNumber,
            NeedSamplingFrq,
            NeedDate,
            NeedTime,
            NeedChannelsNames,
            NeedValues
        }

        public bool TryRead(byte[] data, out FileInfo fileInfo)
        {
            fileInfo = new FileInfo();

            Stream stream = new MemoryStream(data);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var file = new StreamReader(stream, Encoding.GetEncoding("windows-1251")))
            {
                var curState = ParseState.NeedChannelNumber;

                int curRow = 0;
                while (!file.EndOfStream)
                {
                    string curLine;
                    curLine = file.ReadLine();
                    string trimmed = curLine.Trim();

                    if (trimmed[0] == '#')
                    {
                        continue;
                    }

                    switch (curState)
                    {
                        case ParseState.NeedChannelNumber:
                            {
                                if (int.TryParse(trimmed, out fileInfo.nChannels))
                                {
                                    fileInfo.channelNames = new string[fileInfo.nChannels];
                                }
                                else
                                {
                                    return false;
                                }
                                break;
                            }
                        case ParseState.NeedSamplesNumber:
                            {
                                if (int.TryParse(trimmed, out int samplesCount))
                                {
                                    fileInfo.data = new double[samplesCount, fileInfo.nChannels];
                                }
                                else
                                {
                                    return false;
                                }
                                break;
                            }
                        case ParseState.NeedSamplingFrq:
                            {
                                if (!double.TryParse(trimmed, NumberStyles.AllowThousands | NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out fileInfo.nSamplesPerSec))
                                {
                                    return false;
                                }
                                break;
                            }
                        case ParseState.NeedDate:
                            {
                                if (!DateTime.TryParseExact(trimmed, "dd-MM-yyyy", CultureInfo.InvariantCulture,
                                    DateTimeStyles.None, out fileInfo.dateTime))
                                {
                                    return false;
                                }
                                break;
                            }
                        case ParseState.NeedTime:
                            {
                                if (TimeSpan.TryParse(trimmed, CultureInfo.InvariantCulture, out var ts))
                                {
                                    fileInfo.dateTime += ts;
                                }
                                else
                                {
                                    return false;
                                }
                                break;
                            }
                        case ParseState.NeedChannelsNames:
                            {
                                int curStart = 0;
                                int curOffset = 0;

                                for (int i = 0; i < fileInfo.nChannels; i++)
                                {
                                    while (curStart + curOffset < trimmed.Length && trimmed[curStart + curOffset] != ';')
                                    {
                                        curOffset++;
                                    }

                                    fileInfo.channelNames[i] = trimmed[curStart..(curStart + curOffset)];
                                    curStart += curOffset + 1;
                                    curOffset = 0;
                                }

                                break;
                            }
                        case ParseState.NeedValues:
                            {
                                int curStart = 0;
                                int curOffset = 0;

                                for (int i = 0; i < fileInfo.nChannels; i++)
                                {
                                    while (curStart + curOffset < trimmed.Length && trimmed[curStart + curOffset] != ' ')
                                    {
                                        curOffset++;
                                    }

                                    var indice = trimmed[curStart..(curStart + curOffset)];
                                    if (!double.TryParse(indice, NumberStyles.AllowThousands | NumberStyles.Float,
                                        CultureInfo.InvariantCulture, out fileInfo.data[curRow, i]))
                                    {
                                        return false;
                                    }
                                    curStart += curOffset + 1;
                                    curOffset = 0;
                                }

                                curRow++;
                                curState--;
                                break;
                            }
                    }

                    curState++;
                }
            }

            return true;
        }
    }
}
