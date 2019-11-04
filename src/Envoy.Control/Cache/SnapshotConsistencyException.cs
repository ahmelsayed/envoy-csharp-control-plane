using System;

namespace Envoy.Control.Cache
{
    public class SnapshotConsistencyException : Exception
    {
        public SnapshotConsistencyException(string message) : base(message) { }
    }
}