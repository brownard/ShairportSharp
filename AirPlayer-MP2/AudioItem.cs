using AirPlayer.Common.Player;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using MediaPortal.Common.SystemResolver;
using ShairportSharp.Raop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirPlayer.MediaPortal2
{
    class AudioItem : MediaItem
    {
        PlayerSettings playerSettings;

        public AudioItem(PlayerSettings playerSettings)
            : base(Guid.Empty, new Dictionary<Guid, MediaItemAspect>()
			{
				{ ProviderResourceAspect.ASPECT_ID, new MediaItemAspect(ProviderResourceAspect.Metadata)},
				{ MediaAspect.ASPECT_ID, new MediaItemAspect(MediaAspect.Metadata) },
				{ AudioAspect.ASPECT_ID, new MediaItemAspect(AudioAspect.Metadata) }
			})
        {
            this.playerSettings = playerSettings;
            Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, ServiceRegistration.Get<ISystemResolver>().LocalSystemId);
            Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RawUrlResourceProvider.ToProviderResourcePath(AirplayAudioPlayer.DUMMY_FILE).Serialize());
            Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_MIME_TYPE, AirplayAudioPlayer.MIMETYPE);
        }

        public PlayerSettings PlayerSettings { get { return playerSettings; } }

        public void SetMetaData(DmapData metaData)
        {
            Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_TITLE, metaData.Track);
            MediaItemAspect audioAspect = Aspects[AudioAspect.ASPECT_ID];
            audioAspect.SetAttribute(AudioAspect.ATTR_ALBUM, metaData.Album);
            audioAspect.SetCollectionAttribute(AudioAspect.ATTR_ARTISTS, new[] { metaData.Artist });
            audioAspect.SetCollectionAttribute(AudioAspect.ATTR_GENRES, new[] { metaData.Genre });
        }

        public void SetCover(byte[] imageData)
        {
            MediaItemAspect.SetAttribute(Aspects, ThumbnailLargeAspect.ATTR_THUMBNAIL, imageData);
        }
    }
}