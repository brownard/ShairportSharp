using AirPlayer.MediaPortal2.Players;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using MediaPortal.Common.SystemResolver;
using ShairportSharp.Mirroring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirPlayer.MediaPortal2.MediaItems
{
    class MirroringItem : MediaItem
    {
        MirroringStream mirroringStream;

        public MirroringItem(MirroringStream mirroringStream)
            : base(Guid.Empty, new Dictionary<Guid, MediaItemAspect>()
        {
				{ ProviderResourceAspect.ASPECT_ID, new MediaItemAspect(ProviderResourceAspect.Metadata)},
				{ MediaAspect.ASPECT_ID, new MediaItemAspect(MediaAspect.Metadata) },
				{ VideoAspect.ASPECT_ID, new MediaItemAspect(VideoAspect.Metadata) }
			})
        {
            this.mirroringStream = mirroringStream;
            Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, ServiceRegistration.Get<ISystemResolver>().LocalSystemId);
            Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RawUrlResourceProvider.ToProviderResourcePath(AirplayMirroringPlayer.DUMMY_FILE).Serialize());
            Aspects[MediaAspect.ASPECT_ID].SetAttribute(MediaAspect.ATTR_MIME_TYPE, AirplayMirroringPlayer.MIMETYPE);
        }

        public MirroringStream MirroringStream
        {
            get { return mirroringStream; }
        }
    }
}