using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;

namespace AirPlayer.MediaPortal2
{
	public class VideoItem : MediaItem
	{
        public VideoItem(string resolvedPlaybackUrl)
            : base(Guid.Empty, new Dictionary<Guid, MediaItemAspect>()
			{
				{ ProviderResourceAspect.ASPECT_ID, new MediaItemAspect(ProviderResourceAspect.Metadata)},
				{ MediaAspect.ASPECT_ID, new MediaItemAspect(MediaAspect.Metadata) },
				{ VideoAspect.ASPECT_ID, new MediaItemAspect(VideoAspect.Metadata) }
			})
        {
            Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, ServiceRegistration.Get<ISystemResolver>().LocalSystemId);

            Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RawUrlResourceProvider.ToProviderResourcePath(resolvedPlaybackUrl).Serialize());
            Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_MIME_TYPE, AirplayVideoPlayer.MIMETYPE);
        }                
	}
}
