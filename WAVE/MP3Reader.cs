using FFmpeg.AutoGen;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace FileFormats
{
    public unsafe class MP3Reader : IReader
    {
        private const int AUDIO_INBUF_SIZE = 20480;
        private const int AUDIO_REFILL_THRESH = 4096;

        static MP3Reader()
        {
            ffmpeg.RootPath = @"ffmpeg-4.3-win64-shared-lgpl/bin";
            ffmpeg.av_register_all();
            ffmpeg.avcodec_register_all();
            ffmpeg.avformat_network_init();
        }

        public bool TryRead(byte[] indata, out FileInfo fileInfo)
        {
            var instream = new MemoryStream(indata);
            var outstream = new MemoryStream();

            fileInfo = new FileInfo();

            AVCodec* codec;
            AVCodecContext* c = null;
            AVCodecParserContext* parser = null;
            int len, ret;

            byte[] inbuf = new byte[AUDIO_INBUF_SIZE + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE];
            byte* data;
            int data_size;
            int total_samples = 0;
            AVPacket* pkt;
            AVFrame* decoded_frame = null;

            pkt = ffmpeg.av_packet_alloc();

            codec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_MP3);
            if (codec == null)
            {
                return false;
            }
            parser = ffmpeg.av_parser_init((int)codec->id);
            if (parser == null)
            {
                return false;
            }
            c = ffmpeg.avcodec_alloc_context3(codec);
            if (c == null)
            {
                return false;
            }
            if (ffmpeg.avcodec_open2(c, codec, null) < 0)
            {
                return false;
            }

            fixed (byte* ptr = inbuf)
            {
                data = ptr;
                data_size = instream.Read(inbuf, 0, AUDIO_INBUF_SIZE);

                while (data_size > 0)
                {
                    if (decoded_frame == null)
                    {
                        if ((decoded_frame = ffmpeg.av_frame_alloc()) == null)
                        {
                            return false;
                        }
                    }

                    ret = ffmpeg.av_parser_parse2(parser, c, &pkt->data, &pkt->size,
                                   data, data_size,
                                   ffmpeg.AV_NOPTS_VALUE, ffmpeg.AV_NOPTS_VALUE, 0);
                    if (ret < 0)
                    {
                        return false;
                    }

                    data += ret;
                    data_size -= ret;

                    if (pkt->size != 0)
                    {
                        decode(c, pkt, decoded_frame, outstream, ref total_samples);
                    }

                    if (data_size < AUDIO_REFILL_THRESH)
                    {
                        Marshal.Copy((IntPtr)data, inbuf, 0, data_size);
                        
                        data = ptr;
                        len = instream.Read(inbuf, data_size, AUDIO_INBUF_SIZE - data_size);

                        if (len > 0) data_size += len;
                    }
                }
            }

            pkt->data = null;
            pkt->size = 0;
            decode(c, pkt, decoded_frame, outstream, ref total_samples);

            fileInfo.nChannels = c->channels;
            fileInfo.channelNames = new string[fileInfo.nChannels];
            fileInfo.nSamplesPerSec = c->sample_rate;
            fileInfo.data = new double[total_samples, fileInfo.nChannels];

            byte[] rawData = outstream.GetBuffer();
            int bps = ffmpeg.av_get_bytes_per_sample(c->sample_fmt);
            for (int i = 0; i < total_samples; i++)
            {
                for (int j = 0; j < fileInfo.nChannels; j++)
                {
                    switch (bps)
                    {
                        case 2:
                            fileInfo.data[i, j] = BitConverter.ToInt16(rawData, i * bps * fileInfo.nChannels + j * bps);
                            break;
                        case 4:
                            fileInfo.data[i, j] = BitConverter.ToInt32(rawData, i * bps * fileInfo.nChannels + j * bps);
                            break;
                        case 8:
                            fileInfo.data[i, j] = BitConverter.ToInt64(rawData, i * bps * fileInfo.nChannels + j * bps);
                            break;
                        default:
                            return false;
                    }
                }
            }

            instream.Close();
            outstream.Close();

            ffmpeg.avcodec_free_context(&c);
            ffmpeg.av_parser_close(parser);
            ffmpeg.av_frame_free(&decoded_frame);
            ffmpeg.av_packet_free(&pkt);

            return true;
        }

        private bool decode(AVCodecContext* dec_ctx, AVPacket* pkt, AVFrame* frame, MemoryStream outstream, ref int total_samples)
        {
            int i, ch;
            int ret, data_size;

            ret = ffmpeg.avcodec_send_packet(dec_ctx, pkt);
            if (ret < 0)
            {
                return false;
            }

            while (ret >= 0)
            {
                ret = ffmpeg.avcodec_receive_frame(dec_ctx, frame);
                if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                    return true;
                else if (ret < 0)
                {
                    return false;
                }
                data_size = ffmpeg.av_get_bytes_per_sample(dec_ctx->sample_fmt);
                if (data_size < 0)
                {
                    return false;
                }

                total_samples += frame->nb_samples;
                for (i = 0; i < frame->nb_samples; i++)
                {
                    for (ch = 0; ch < dec_ctx->channels; ch++)
                    {
                        byte* curValPtr = frame->data[(uint)ch] + data_size * i;
                        byte[] curVal = new byte[data_size];
                        Marshal.Copy((IntPtr)curValPtr, curVal, 0, data_size);

                        outstream.Write(curVal, 0, data_size);
                    }
                }
            }

            return true;
        }
    }
}
