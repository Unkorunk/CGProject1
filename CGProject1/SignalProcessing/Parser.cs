using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace CGProject1 {
    public class Parser {
        private enum ParseState {
            NeedChannelNumber,
            NeedSamplesNumber,
            NeedSamplingFrq,
            NeedDate,
            NeedTime,
            NeedChannelsNames,
            NeedValues
        }

        public static Signal Parse(string filePath) {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Signal result;
            
            using (var file = new StreamReader(filePath, Encoding.GetEncoding("windows-1251"))) {
                var curState = ParseState.NeedChannelNumber;
                result = new Signal();
                result.fileName = Path.GetFileName(filePath);

                int curRow = 0;

                while (!file.EndOfStream) {
                    string curLine;
                    curLine = file.ReadLine();
                    string trimmed = curLine.Trim();

                    if (trimmed[0] == '#') {
                        continue;
                    }

                    switch (curState) {
                        case ParseState.NeedChannelNumber: {
                                int channelsNum = Int32.Parse(trimmed);
                                result.channels = new Channel[channelsNum];
                                break;
                            }
                        case ParseState.NeedSamplesNumber: {
                                int samplesCount = Int32.Parse(trimmed);

                                for (int i = 0; i < result.channels.Length; i++) {
                                    result.channels[i] = new Channel(samplesCount, result.fileName);
                                }

                                break;
                            }
                        case ParseState.NeedSamplingFrq: {
                                result.SamplingFrq = double.Parse(trimmed, CultureInfo.InvariantCulture);
                                break;
                            }
                        case ParseState.NeedDate: {
                                result.StartDateTime = DateTime.ParseExact(trimmed, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                                break;
                            }
                        case ParseState.NeedTime: {
                                var ts = TimeSpan.ParseExact(trimmed, "hh\\:mm\\:ss\\.fff", CultureInfo.InvariantCulture);
                                result.StartDateTime = result.StartDateTime + ts;
                                break;
                            }
                        case ParseState.NeedChannelsNames: {
                                int curStart = 0;
                                int curOffset = 0;

                                foreach (var channel in result.channels) {
                                    while (curStart + curOffset < trimmed.Length && trimmed[curStart + curOffset] != ';') {
                                        curOffset++;
                                    }

                                    channel.Name = trimmed[curStart..(curStart + curOffset)];
                                    curStart += curOffset + 1;
                                    curOffset = 0;
                                }

                                break;
                            }
                        case ParseState.NeedValues: {
                                int curStart = 0;
                                int curOffset = 0;

                                foreach (var channel in result.channels) {
                                    while (curStart + curOffset < trimmed.Length && trimmed[curStart + curOffset] != ' ') {
                                        curOffset++;
                                    }

                                    var indice = trimmed[curStart..(curStart + curOffset)];
                                    channel.values[curRow] = double.Parse(indice, CultureInfo.InvariantCulture);
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

            return result;
        }
    }
}
