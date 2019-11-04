using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Google.Protobuf;

namespace Envoy.Control.Cache
{
    public class SnapshotResources<T> where T : IMessage
    {
        public IReadOnlyDictionary<string, T> Resources { get; }
        public Func<IEnumerable<string>, string> VersionResolver { get; }
        public string Version => this.VersionResolver(Enumerable.Empty<string>());

        public SnapshotResources(IEnumerable<T> resources, string version)
                : this(resources, _ => version) { }

        public SnapshotResources(IEnumerable<T> resources, Func<IEnumerable<string>, string> versionResolver)
        {
            this.Resources = resources.ToDictionary(k => Cache.Resources.GetResourceName(k), v => v);
            this.VersionResolver = versionResolver;
        }

        public string GetVersion(string resourceNames)
            => this.GetVersion(new[] { resourceNames });
        public string GetVersion(IEnumerable<string> resourceNames)
            => this.VersionResolver(resourceNames);
    }
}