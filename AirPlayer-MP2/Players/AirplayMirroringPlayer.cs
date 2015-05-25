using AirPlayer.Common.DirectShow;
using DirectShow;
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

        MirroringStream stream;
        MirroringSourceFilter sourceFilter;

        public AirplayMirroringPlayer(MirroringStream stream)
        {
            this.stream = stream;
        }

        #region Graph Building

        protected override void AddSourceFilter()
        {
            sourceFilter = new MirroringSourceFilter(stream);
            _graphBuilder.AddFilter(sourceFilter, sourceFilter.Name);
        }

        protected override void OnBeforeGraphRunning()
        {
            if (sourceFilter != null)
            {
                using (DSFilter dsFilter = new DSFilter(sourceFilter))
                    _graphBuilder.Render(dsFilter.OutputPin.Value);
                FilterGraphUtils.SetQualityControl(_graphBuilder, sourceFilter);
            }
        }

        protected override void AddAudioRenderer()
        {
            //no need for audio renederer
        }

        protected override void FreeCodecs()
        {
            if (sourceFilter != null)
            {
                //base.FreeCodecs tries to release our filter as a COM object which throws because it's not
                //remove from _streamSelectors to prevent this
                _streamSelectors.Remove(sourceFilter as IAMStreamSelect);
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
