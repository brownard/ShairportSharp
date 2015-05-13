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
    public class MirroringSourceFilter : BaseSourceFilterTemplate<MirroringFileParser>
    {
        public MirroringSourceFilter(MirroringStream stream)
            : base("MirroringSourceFilter")
        {
            ((MirroringFileParser)m_Parsers[0]).SetSource(stream, this);
            m_sFileName = "http://localhost/Shairport";
            Load(m_sFileName, null);
        }
    }

    public class MirroringFileParser : FileParser
    {
        protected MirroringStream stream;
        protected BaseFilter filter;

        public MirroringFileParser() : base(false) { }

        public BaseFilter Filter
        {
            get { return filter; }
        }

        public void SetSource(MirroringStream stream, BaseFilter filter)
        {
            this.stream = stream;
            this.filter = filter;
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

        //public override HRESULT ProcessDemuxPackets()
        //{
        //    MirroringMessage[] packets = stream.TakeAllPackets();
        //    if (packets == null)
        //        return S_FALSE;

        //    foreach (MirroringMessage packet in packets)
        //    {
        //        ExtendedPacketData packetData = new ExtendedPacketData();
        //        packetData.Buffer = packet.NALUs;
        //        packetData.Size = packet.NALUs.Length;
        //        packetData.Start = 0;
        //        packetData.Stop = 0;
        //        if (!firstSample)
        //        {
        //            if (packet.PayloadType == PayloadType.Codec)
        //                packetData.SetSPS(packet.CodecData.SPS);
        //        }
        //        else 
        //        {
        //            firstSample = false;
        //        }
        //        PacketData pd = (PacketData)packetData;
        //        m_Tracks[0].AddToCache(ref pd);
        //    }
        //    return S_OK;
        //}
    }

    class MirrorDemuxTrack : DemuxTrack
    {
        protected MirroringStream stream;
        MirroringPacket[] packetCache;
        int currentPacketIndex;
        bool firstSample = true;
        AMMediaType pmt;
        BaseFilter filter;

        public MirrorDemuxTrack(MirroringFileParser parser, MirroringStream stream)
            : base(parser, TrackType.Video)
        {
            this.stream = stream;
            pmt = getMediaType(stream.CodecData);
            filter = parser.Filter;
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
                pmt.Set(this.pmt);
                return NOERROR;
            }
            return VFW_S_NO_MORE_ITEMS;
        }

        public override HRESULT ReadMediaSample(ref IMediaSampleImpl pSample)
        {
            MirroringPacket mirroringPacket = getNextMirroringPacket();
            if (mirroringPacket == null)
                return S_FALSE;
            
            PacketData _packet = getPacketData(mirroringPacket);
            if (!firstSample && mirroringPacket.CodecData != null)
                pSample.SetMediaType(getMediaType(mirroringPacket.CodecData));

            firstSample = false;
            pSample.SetMediaTime(null, null);
            pSample.SetPreroll(false);
            pSample.SetDiscontinuity(false);
            pSample.SetSyncPoint(_packet.SyncPoint);
            pSample.SetTime(_packet.Start, _packet.Stop);

            IntPtr pBuffer;
            pSample.GetPointer(out pBuffer);
            int _readed = 0;
            ASSERT(pSample.GetSize() >= _packet.Size);

            _readed = FillSampleBuffer(pBuffer, pSample.GetSize(), ref _packet);
            _packet.Dispose();
            pSample.SetActualDataLength(_readed);

            m_bFirstSample = false;

            if (_readed == 0) return S_FALSE;

            return NOERROR;
        }

        AMMediaType getMediaType(H264CodecData codecData)
        {
            SPSUnit spsUnit = new SPSUnit(codecData.SPS);
            int width = spsUnit.Width();
            int height = spsUnit.Height();

            VideoInfoHeader2 vi = new VideoInfoHeader2();
            vi.SrcRect.right = width;
            vi.SrcRect.bottom = height;
            vi.TargetRect.right = width;
            vi.TargetRect.bottom = height;

            int hcf = HCF(width, height);
            vi.PictAspectRatioX = width / hcf;
            vi.PictAspectRatioY = height / hcf;
            vi.BmiHeader.Width = width;
            vi.BmiHeader.Height = height;
            vi.BmiHeader.Planes = 1;
            vi.BmiHeader.Compression = 0x34363248;

            AMMediaType amt = new AMMediaType();
            amt.majorType = MediaType.Video;
            amt.subType = MediaSubType.H264;
            amt.temporalCompression = true;
            amt.fixedSizeSamples = false;
            amt.sampleSize = 1;
            amt.SetFormat(vi);
            return amt;
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

        PacketData getPacketData(MirroringPacket packet)
        {
            PacketData packetData = new PacketData();
            packetData.Buffer = packet.Nalus;
            packetData.Size = packet.Nalus.Length;
            if (filter.State == FilterState.Running)
            {
                long streamTime;
                filter.StreamTime(out streamTime);
                //schedule for 100ms in the future
                packetData.Start = (100 * UNITS / MILLISECONDS) + streamTime;
            }
            else
            {
                packetData.Start = 0;
            }
            packetData.Stop = packetData.Start + 1;
            return packetData;
        }

        static long convertToDSTime(ulong ntpTime)
        {
            long seconds = (uint)((ntpTime >> 32) & 0xFFFFFFFF) * UNITS;
            uint fraction = (uint)(ntpTime & 0xFFFFFFFF);
            long fractionSecs = (long)(((double)fraction / uint.MaxValue) * UNITS);
            return seconds + fractionSecs;
        }

        static int HCF(int x, int y)
        {
            return y == 0 ? x : HCF(y, x % y);
        }

        //const int FOURCC_AVC1 = 0x31435641;
        //static readonly Guid SUBTYPE_AVC1 = new Guid(0x31435641, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);
        //AMMediaType getMediaType(H264CodecData codecData)
        //{
        //    Mpeg2VideoInfo vi = new Mpeg2VideoInfo();
        //    vi.hdr.SrcRect.right = 432;
        //    vi.hdr.SrcRect.bottom = 648;
        //    vi.hdr.TargetRect.right = 432;
        //    vi.hdr.TargetRect.bottom = 648;
        //    vi.hdr.AvgTimePerFrame = 400000;
        //    vi.hdr.BmiHeader.Width = 432;
        //    vi.hdr.BmiHeader.Height = 648;
        //    vi.hdr.BmiHeader.Planes = 1;
        //    vi.hdr.BmiHeader.Compression = FOURCC_AVC1;

        //    vi.dwProfile = (uint)codecData.Profile;
        //    vi.dwLevel = (uint)codecData.Level;
        //    vi.dwFlags = (uint)codecData.NALSizeMinusOne + 1;

        //    int offset = 0;
        //    byte[] sp = new byte[codecData.SPSLength + codecData.PPSLength + 4];
        //    sp[offset++] = (byte)(codecData.SPSLength >> 8);
        //    sp[offset++] = (byte)codecData.SPSLength;
        //    for (int i = 0; i < codecData.SPSLength; i++)
        //        sp[offset++] = codecData.SPS[i];
        //    sp[offset++] = (byte)(codecData.PPSLength >> 8);
        //    sp[offset++] = (byte)(codecData.PPSLength);
        //    for (int i = 0; i < codecData.PPSLength; i++)
        //        sp[offset++] = codecData.PPS[i];

        //    vi.cbSequenceHeader = (uint)sp.Length;

        //    AMMediaType amt = new AMMediaType();
        //    amt.majorType = MediaType.Video;
        //    amt.subType = SUBTYPE_AVC1;
        //    amt.temporalCompression = true;
        //    amt.fixedSizeSamples = false;
        //    amt.sampleSize = 1;
        //    setFormat(vi, sp, amt);
        //    return amt;
        //}

        //void setFormat(Mpeg2VideoInfo vi, byte[] sp, AMMediaType amt)
        //{
        //    int cb = Marshal.SizeOf(vi);
        //    int add = sp == null || sp.Length < 4 ? 0 : sp.Length - 4;
        //    IntPtr _ptr = Marshal.AllocCoTaskMem(cb + add);
        //    try
        //    {
        //        Marshal.StructureToPtr(vi, _ptr, true);
        //        if (sp != null)
        //            Marshal.Copy(sp, 0, _ptr + cb - 4, sp.Length);
        //        amt.SetFormat(_ptr, cb + add);
        //        amt.formatType = FormatType.Mpeg2Video;
        //    }
        //    finally
        //    {
        //        Marshal.FreeCoTaskMem(_ptr);
        //    }
        //}
    }
}