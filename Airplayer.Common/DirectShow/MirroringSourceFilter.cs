using AirPlayer.Common.H264;
using DirectShow;
using DirectShow.BaseClasses;
using DirectShow.Helper;
using ShairportSharp.Mirroring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AirPlayer.Common.DirectShow
{
    public class MirroringSourceFilter : BaseSourceFilterTemplate<MirroringFileParser>, IQualityControl
    {
        public MirroringSourceFilter(MirroringStream stream, int timestampOffset = -1)
            : base("MirroringSourceFilter")
        {
            ((MirroringFileParser)m_Parsers[0]).SetSource(stream, this, timestampOffset);
            m_sFileName = "http://localhost/Shairport";
            Load(m_sFileName, null);
        }

        #region IQualityControl

        public int Notify(IntPtr pSelf, Quality q)
        {
            return 0;
        }

        public int SetSink(IntPtr piqc)
        {
            return 0;
        }

        public void SetQualityControl(IGraphBuilder graphBuilder)
        {
            IEnumFilters pEnum;
            if (SUCCEEDED(graphBuilder.EnumFilters(out pEnum)))
            {
                IBaseFilter[] aFilters = new IBaseFilter[1];
                while (S_OK == pEnum.Next(1, aFilters, IntPtr.Zero))
                {
                    using (var filter = new DSFilter(aFilters[0]))
                    {
                        if (isVideoRenderer(filter))
                        {
                            IntPtr pqc = Marshal.GetComInterfaceForObject(this, typeof(IQualityControl));
                            ((IQualityControl)filter.InputPin.Value).SetSink(pqc);
                            Marshal.Release(pqc);
                            break;
                        }
                    }
                }
                Marshal.ReleaseComObject(pEnum);
            }
        }

        static bool isVideoRenderer(DSFilter filter)
        {
            return filter.OutputPin == null && filter.InputPin != null && filter.InputPin.ConnectionMediaType != null && filter.InputPin.ConnectionMediaType.majorType == MediaType.Video;
        }

        #endregion
    }

    public class MirroringFileParser : FileParser
    {
        protected MirroringStream stream;
        protected MirroringSourceFilter filter;
        protected int timestampOffset;

        public MirroringFileParser() : base(false) { }

        public BaseFilter Filter
        {
            get { return filter; }
        }

        public int TimestampOffset
        {
            get { return timestampOffset; }
        }

        public void SetSource(MirroringStream stream, MirroringSourceFilter filter, int timestampOffset)
        {
            this.stream = stream;
            this.filter = filter;
            this.timestampOffset = timestampOffset;
        }

        protected override HRESULT CheckFile()
        {
            return S_OK;
        }

        protected override HRESULT LoadTracks()
        {
            m_Tracks.Add(new MirrorDemuxTrack(this, stream));
            return S_OK;
        }
    }

    class MirrorDemuxTrack : DemuxTrack
    {
        MirroringFileParser parser;
        MirroringStream stream;
        H264CodecData codecData;
        MirroringPacket[] packetCache;
        int currentPacketIndex;

        public MirrorDemuxTrack(MirroringFileParser parser, MirroringStream stream)
            : base(parser, TrackType.Video)
        {
            this.parser = parser;
            this.stream = stream;
            codecData = stream.CodecData;
        }

        public override HRESULT GetTrackAllocatorRequirements(ref int plBufferSize, ref short pwBuffers)
        {
            plBufferSize = 256000;
            pwBuffers = 10;
            return S_OK;
        }

        public override HRESULT GetMediaType(int iPosition, ref AMMediaType pmt)
        {
            if (iPosition == 0)
            {
                pmt.Set(getMediaType(codecData));
                return NOERROR;
            }
            return VFW_S_NO_MORE_ITEMS;
        }

        AMMediaType getMediaType(H264CodecData codecData)
        {
            return MediaTypeBuilder.H264(codecData);
        }

        public override HRESULT ReadMediaSample(ref IMediaSampleImpl pSample)
        {
            MirroringPacket mirroringPacket = getNextMirroringPacket();
            if (mirroringPacket == null)
                return S_FALSE;

            byte[] buffer = GetNalus(mirroringPacket);
            if (!m_bFirstSample && mirroringPacket.CodecData != null)
                pSample.SetMediaType(getMediaType(mirroringPacket.CodecData));

            pSample.SetMediaTime(null, null);
            pSample.SetPreroll(false);
            pSample.SetDiscontinuity(false);
            pSample.SetSyncPoint(false);

            long start, stop;
            GetTimestamps(out start, out stop);
            pSample.SetTime(start, stop);

            IntPtr pBuffer;
            pSample.GetPointer(out pBuffer);
            int _read = 0;
            ASSERT(pSample.GetSize() >= buffer.Length);

            _read = FillSampleBuffer(pBuffer, pSample.GetSize(), buffer);
            pSample.SetActualDataLength(_read);

            m_bFirstSample = false;

            if (_read == 0) return S_FALSE;

            return NOERROR;
        }

        protected int FillSampleBuffer(IntPtr pBuffer, int _size, byte[] buffer)
        {
            int _read = 0;
            if (buffer != null)
            {
                _read = buffer.Length <= _size ? buffer.Length : _size;
                Marshal.Copy(buffer, 0, pBuffer, _read);
            }
            return _read;
        }
                
        protected byte[] GetNalus(MirroringPacket packet)
        {
            //convert the AVCC packets into Annex B as decoders seem to handle
            //resolution/orientation changes better
            byte[] nalus;
            if (packet.CodecData != null)
            {
                codecData = packet.CodecData;
                nalus = NaluParser.CreateParameterSet(codecData.SPS, codecData.PPS);
            }
            else
            {
                nalus = NaluParser.ParseNalus(packet.Nalus, codecData.NALSizeMinusOne + 1);
            }
            return nalus;
        }

        protected void GetTimestamps(out long start, out long stop)
        {
            if (parser.TimestampOffset > -1)
            {
                if (parser.Filter.State == FilterState.Running)
                {
                    long streamTime;
                    parser.Filter.StreamTime(out streamTime);
                    //schedule in the future
                    start = (parser.TimestampOffset * UNITS / MILLISECONDS) + streamTime;
                    stop = start + 1;
                }
                else
                {
                    start = 0;
                    stop = 0;
                }
            }
            else
            {
                start = 0;
                stop = 0;
            }
        }

        MirroringPacket getNextMirroringPacket()
        {
            if (packetCache == null || currentPacketIndex == packetCache.Length)
            {
                currentPacketIndex = 0;
                packetCache = stream.TakeAllPackets();
                if (packetCache == null)
                    return null;
            }
            return packetCache[currentPacketIndex++];
        }

        static long convertToDSTime(ulong ntpTime)
        {
            long seconds = (uint)((ntpTime >> 32) & 0xFFFFFFFF) * UNITS;
            uint fraction = (uint)(ntpTime & 0xFFFFFFFF);
            long fractionSecs = (long)(((double)fraction / uint.MaxValue) * UNITS);
            return seconds + fractionSecs;
        }

        static DsLong sanitize(long time)
        {
            if (time < 0)
                return null;
            return time;
        }
    }
}