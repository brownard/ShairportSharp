using DirectShow;
using DirectShow.BaseClasses;
using DirectShow.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AirPlayer.Common.DirectShow
{
    public class GenericPushSourceFilter : BaseSourceFilterTemplate<GenericFileParser>
    {
        public GenericPushSourceFilter(Stream stream, AMMediaType pmt)
            : base("GenericPullSourceFilter")
        {
            ((GenericFileParser)m_Parsers[0]).SetSource(stream, pmt);
            m_sFileName = "Shairport.m4a";
            Load(m_sFileName, pmt);
        }
    }

    public class GenericFileParser : FileParser
    {
        protected Stream stream;
        protected AMMediaType pmt;

        public GenericFileParser()
            : base(false)
        {
        }

        public void SetSource(Stream stream, AMMediaType pmt)
        {
            this.stream = stream;
            this.pmt = pmt;
        }

        protected override HRESULT CheckFile()
        {
            return S_OK;
        }

        protected override HRESULT LoadTracks()
        {
            m_Tracks.Add(new GenericDemuxTrack(this, stream, pmt));
            return S_OK;
        }
    }

    class GenericDemuxTrack : DemuxTrack
    {
        const int BUFFER_SIZE = 2048;
        protected Stream stream;
        protected AMMediaType pmt;
        public GenericDemuxTrack(FileParser parser, Stream stream, AMMediaType pmt)
            : base(parser, TrackType.Audio)
        {
            this.stream = stream;
            this.pmt = pmt;
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

        public override PacketData GetNextPacket()
        {
            byte[] buffer = new byte[BUFFER_SIZE];
            int read = stream.Read(buffer, 0, buffer.Length);
            if (read > 0)
            {
                PacketData packetData = new PacketData();
                packetData.Buffer = buffer;
                packetData.Size = read;
                packetData.SyncPoint = true;
                return packetData;
            }
            return null;
        }
    }
}
