using AirPlayer.Common.DirectShow;
using DirectShow;
using DirectShow.Helper;
using ShairportSharp.Airplay;
using ShairportSharp.Mirroring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ShairportSharp_Test
{
    public class RotPlayer : DSFilePlayback
    {
        protected DsROTEntry _rot;

        protected override HRESULT OnInitInterfaces()
        {
            _rot = new DsROTEntry(m_GraphBuilder);
            return base.OnInitInterfaces();
        }

        public override void Dispose()
        {
            if (_rot != null)
            {
                try
                {
                    _rot.Dispose();
                }
                catch { }
                _rot = null;
            }
            base.Dispose();
        }
    }

    public class MirrorPlayer : RotPlayer
    {
        const string LAV_GUID = "{EE30215D-164F-4A92-A4EB-9D4C13390F9F}";
        const string MS_GUID = "{212690FB-83E5-4526-8FD7-74478B7939CD}";

        MirroringStream stream;

        public MirrorPlayer(MirroringStream stream)
        {
            this.stream = stream;
        }

        protected override HRESULT OnInitInterfaces()
        {
            _rot = new DsROTEntry(m_GraphBuilder);

            var sourceFilter = new MirroringSourceFilter(stream);
            int hr = m_GraphBuilder.AddFilter(sourceFilter, sourceFilter.Name);
            new HRESULT(hr).Throw();

            using (DSFilter video = new DSFilter(new Guid(MS_GUID)))
                hr = m_GraphBuilder.AddFilter(video.Value, video.Name);

            using (DSFilter source2 = new DSFilter(sourceFilter))
                hr = m_GraphBuilder.Render(source2.OutputPin.Value);

            FilterGraphUtils.SetQualityControl(m_GraphBuilder, sourceFilter);
            return new HRESULT(hr);
        }

        public override void Dispose()
        {
            if (stream != null)
            {
                stream.Stop();
                stream = null;
            }
            base.Dispose();
        }
    }
}