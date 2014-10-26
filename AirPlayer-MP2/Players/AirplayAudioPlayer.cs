using AirPlayer.Common.DirectShow;
using AirPlayer.Common.Player;
using AirPlayer.MediaPortal2.MediaItems;
using DirectShow.Helper;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Presentation.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirPlayer.MediaPortal2.Players
{
    public class AirplayAudioPlayer : BaseDXPlayer, MediaPortal.UI.Presentation.Players.IAudioPlayer, Common.Player.IAudioPlayer, IReusablePlayer
    {
        public const string MIMETYPE = "airplay-audio/airplayer";
        public const string DUMMY_FILE = "airplay://localhost/AirPlayerAudio.airplay";
        PlayerSettings settings;

        object timestampSync = new object();
        double sampleRate;
        uint startStamp;
        uint stopStamp;
        TimeSpan currentDuration;

        public AirplayAudioPlayer(PlayerSettings settings)
        {
            this.settings = settings;
            sampleRate = (double)settings.Source.SampleRate;
        }

        protected override void AddSourceFilter()
        {
            var sourceFilter = new GenericPushSourceFilter(settings.Source, settings.GetMediaType());
            int hr = _graphBuilder.AddFilter(sourceFilter, sourceFilter.Name);
            new HRESULT(hr).Throw();
            DSFilter source2 = new DSFilter(sourceFilter);
            hr = source2.OutputPin.Render();
            new HRESULT(hr).Throw();
            return;
        }

        public override string Name
        {
            get { return "AirPlayer Audio Player"; }
        }

        public void UpdateDurationInfo(uint startStamp, uint stopStamp)
        {
            lock (timestampSync)
            {
                this.startStamp = startStamp;
                this.stopStamp = stopStamp;
                currentDuration = TimeSpan.FromSeconds((stopStamp - startStamp) / sampleRate);
            }
        }

        public override TimeSpan CurrentTime
        {
            get
            {
                lock (timestampSync)
                {
                    if (currentDuration != null)
                    {
                        double currentTime = (settings.GetLastTimeStamp() - startStamp) / sampleRate;
                        if (currentTime <= currentDuration.TotalSeconds)
                            return TimeSpan.FromSeconds(currentTime);
                    }
                }
                return TimeSpan.Zero;
            }
            set { }
        }

        public override TimeSpan Duration
        {
            get
            {
                lock (timestampSync)
                {
                    if (currentDuration != null)
                        return currentDuration;
                }
                return TimeSpan.Zero;
            }
        }

        public bool NextItem(MediaPortal.Common.MediaManagement.MediaItem mediaItem, StartTime startTime)
        {
            AudioItem audioItem = mediaItem as AudioItem;
            return audioItem != null && audioItem.PlayerSettings == null;
        }

        public event RequestNextItemDlgt NextItemRequest;
    }
}
