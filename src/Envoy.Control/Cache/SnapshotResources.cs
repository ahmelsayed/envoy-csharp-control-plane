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
        public IResourceVersionResolver VersionResolver { get; }
        public string Version => this.VersionResolver.Version();

        public SnapshotResources(IEnumerable<T> resources, string version)
                : this(resources, new ResourceVersionResolver(version)) { }

        public SnapshotResources(IEnumerable<T> resources, IResourceVersionResolver versionResolver)
        {
            this.Resources = resources
            .ToDictionary(k => Cache.Resources.GetResourceName(k), v => v);
            // .ToImmutableDictionary();

            this.VersionResolver = versionResolver;
        }

        public string GetVersion(IEnumerable<string> resourceNames)
            => this.VersionResolver.Version(resourceNames);

        private class ResourceVersionResolver : IResourceVersionResolver
        {
            private readonly string _version;

            public ResourceVersionResolver(string version)
            {
                this._version = version;
            }
            public string Version(IEnumerable<string> resourceNames)
                => this._version;
        }
    }
}