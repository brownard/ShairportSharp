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
            ((MirroringFileParser)m_Parsers[0]).SetSource(stream);
            m_sFileName = "http://localhost/Shairport";
            Load(m_sFileName, null);
        }
    }

    public class MirroringFileParser : FileParser
    {
        const int FOURCC_AVC1 = 0x31435641;
        static readonly Guid SUBTYPE_AVC1 = new Guid(0x31435641, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);
        protected MirroringStream stream;

        public MirroringFileParser() : base(true) { }

        public void SetSource(MirroringStream stream)
        {
            this.stream = stream;
        }

        protected override HRESULT CheckFile()
        {
            return S_OK;
        }

        protected override HRESULT LoadTracks()
        {
            m_Tracks.Add(new MirrorDemuxTrack(this, getMediaType(), stream));
            return S_OK;
        }

        public override HRESULT ProcessDemuxPackets()
        {
            MirroringMessage[] packets = stream.TakeAllPackets();
            if (packets == null)
                return S_FALSE;

            foreach (MirroringMessage packet in packets)
            {
                PacketData packetData = new PacketData();
                packetData.Buffer = packet.Content;
                packetData.Size = packet.Content.Length;
                packetData.Start = 0;
                packetData.Stop = 0;
                m_Tracks[0].AddToCache(ref packetData);
            }
            return S_OK;
        }

        AMMediaType getMediaType()
        {
            VideoInfoHeader2 vi = new VideoInfoHeader2();
            vi.AvgTimePerFrame = 400000;
            vi.BmiHeader.Width = 960;
            vi.BmiHeader.Height = 640;
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

        //AMMediaType getMediaType()
        //{
        //    H264CodecData codecData = stream.CodecData;
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

        //long convertToDSTime(ulong ntpTime)
        //{
        //    long seconds = (uint)((ntpTime >> 32) & 0xFFFFFFFF) * UNITS;
        //    uint fraction = (uint)(ntpTime & 0xFFFFFFFF);
        //    long fractionSecs = (long)(((double)fraction / uint.MaxValue) * UNITS);
        //    return seconds + fractionSecs;
        //}
    }

    class MirrorDemuxTrack : DemuxTrack
    {
        object mediaTypeSync = new object();
        protected AMMediaType pmt;
        protected MirroringStream stream;

        public MirrorDemuxTrack(FileParser parser, AMMediaType pmt, MirroringStream stream)
            : base(parser, TrackType.Video)
        {
            this.pmt = pmt;
            this.stream = stream;
        }

        public override HRESULT GetTrackAllocatorRequirements(ref int plBufferSize, ref short pwBuffers)
        {
            plBufferSize = 256000;
            pwBuffers = 10;
            return S_OK;
        }

        public override HRESULT GetMediaType(int iPosition, ref AMMediaType pmt)
        {
            pmt.Set(this.pmt);
            return NOERROR;
        }

        //public override PacketData GetNextPacket()
        //{
        //    MirroringMessage packet = stream.TakePacket();
        //    if (packet == null)
        //        return null;

        //    PacketData packetData = new PacketData();
        //    packetData.Buffer = packet.Content;
        //    packetData.Size = packet.Content.Length;
        //    packetData.Start = 0;
        //    packetData.Stop = 0;
        //    return packetData;
        //}
    }
}