using System.Collections.Generic;

namespace Envoy.Control.Cache
{
    public interface ICache<T> : IConfigWatcher
    {
        IEnumerable<T> Groups { get; }

        IStatusInfo<T> GetStatusInfo(T group);
    }
}