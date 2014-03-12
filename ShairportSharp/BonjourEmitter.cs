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
        public event NetService.ServicePublished DidPublishService;
        public event NetService.ServiceNotPublished DidNotPublishService;
        protected NetService CurrentService = null;
        protected abstract NetService GetNetService();

        public virtual void Publish()
        {
            if (CurrentService == null)
            {
                CurrentService = GetNetService();
                if (CurrentService != null)
                {
                    CurrentService.AllowMultithreadedCallbacks = true;
                    CurrentService.DidPublishService += OnDidPublishService;
                    CurrentService.DidNotPublishService += OnDidNotPublishService;
                    CurrentService.Publish();
                }
            }
        }

        public virtual void Stop()
        {
            if (CurrentService != null)
            {
                CurrentService.Stop();
                CurrentService.Dispose();
                CurrentService = null;
            }
        }
        
        protected virtual void OnDidPublishService(NetService service)
        {
            Logger.Debug("Bonjour: Published service '{0}'", service.Name);
            if (DidPublishService != null)
                DidPublishService(service);
        }

        protected virtual void OnDidNotPublishService(NetService service, DNSServiceException exception)
        {
            Logger.Debug("Bonjour: Failed to publish service '{0}' - {1}", service.Name, exception.Message);
            if (DidNotPublishService != null)
                DidNotPublishService(service, exception);
        }
    }
}
