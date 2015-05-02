using AirPlayer.Common.DirectShow;
using DirectShow.Helper;
using MediaPortal.UI.Players.Video;
using ShairportSharp.Mirroring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirPlayer.MediaPortal2.Players
{
    class AirplayMirroringPlayer : VideoPlayer
    {
        public const string MIMETYPE = "video/airplayer-mirroring";
        public const string DUMMY_FILE = "airplay://localhost/AirPlayerMirroring.airplay";
        const string LAV_VIDEO_GUID = "{EE30215D-164F-4A92-A4EB-9D4C13390F9F}";

        MirroringStream stream;
        DSFilter sourceFilter;

        public AirplayMirroringPlayer(MirroringStream stream)
        {
            this.stream = stream;
        }

        #region Graph Building

        protected override void AddSourceFilter()
        {
            sourceFilter = new DSFilter(new MirroringSourceFilter(stream));
            _graphBuilder.AddFilter(sourceFilter.Value, sourceFilter.Name);
        }

        protected override void AddPreferredCodecs()
        {
            try
            {
                var lavVideo = new DSFilter(new Guid(LAV_VIDEO_GUID));
                int hr = _graphBuilder.AddFilter(lavVideo.Value, lavVideo.Name);
                lavVideo.Dispose();
                new HRESULT(hr).Throw();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("AirplayMirroringPlayer: Failed to add LAV Video to graph -", ex);
            }
        }

        protected override void OnBeforeGraphRunning()
        {
            if (sourceFilter != null)
                _graphBuilder.Render(sourceFilter.OutputPin.Value);
        }

        protected override void AddAudioRenderer()
        {
            //no need for audio renederer
        }

        protected override void FreeCodecs()
        {
            if (sourceFilter != null)
            {
                sourceFilter.Dispose();
                sourceFilter = null;
            }
            base.FreeCodecs();
        }

        #endregion

        public override void Stop()
        {
            stream.Stop();
            base.Stop();
        }              

        public override bool CanSeekBackwards
        {
            get { return false; }
        }

        public override bool CanSeekForwards
        {
            get { return false; }
        }
    }
}
