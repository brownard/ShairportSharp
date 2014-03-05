using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ZeroconfService;

namespace ShairportSharp
{
    abstract class BonjourEmitter
    {
        NetService currentService = null;
        public event NetService.ServicePublished DidPublishService;
        public event NetService.ServiceNotPublished DidNotPublishService;

        public void Publish()
        {
            currentService = GetNetService();
            currentService.AllowMultithreadedCallbacks = true;
            currentService.DidPublishService += service_DidPublishService;
            currentService.DidNotPublishService += service_DidNotPublishService;
            currentService.Publish();
        }

        public void Stop()
        {
            if (currentService != null)
            {
                currentService.Stop();
                currentService = null;
            }
        }

        protected abstract NetService GetNetService();

        void service_DidPublishService(NetService service)
        {
            Logger.Debug("Bonjour: Published service '{0}'", service.Name);
            if (DidPublishService != null)
                DidPublishService(service);
        }

        void service_DidNotPublishService(NetService service, DNSServiceException exception)
        {
            Logger.Debug("Bonjour: Failed to publish service '{0}' - {1}", service.Name, exception.Message);
            if (DidNotPublishService != null)
                DidNotPublishService(service, exception);
        }
    }
}
