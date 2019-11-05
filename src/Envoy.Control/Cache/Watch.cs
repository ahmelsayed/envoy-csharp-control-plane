using System;
using System.Threading;
using Envoy.Api.V2;

namespace Envoy.Control.Cache
{
    public class Watch
    {
        public bool Ads { get; }
        public DiscoveryRequest Request { get; }
        public Action<Response> ResponseAction { get; }
        private volatile int isCancelled = 0;
        private Action? _stop;

        public Watch(bool ads, DiscoveryRequest request, Action<Response> responseAction)
        {
            this.Ads = ads;
            this.Request = request;
            this.ResponseAction = responseAction;
        }

        public void Cancel()
        {
            if (Interlocked.Exchange(ref isCancelled, 1) == 0)
            {
                this._stop?.Invoke();
            }
        }

        public bool IsCancelled => isCancelled == 1;

        public void Respond(Response response)
        {
            if (IsCancelled)
            {
                throw new WatchCancelledException();
            }

            ResponseAction(response);
        }

        public void SetStop(Action stop)
        {
            this._stop = stop;
        }
    }
}