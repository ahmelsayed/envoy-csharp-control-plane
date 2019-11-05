namespace Envoy.Control.Cache
{
    public interface ISnapshotCache<T> : ICache<T>
    {
        bool ClearSnapshot(T group);
        Snapshot? GetSnapshot(T group);
        void SetSnapshot(T group, Snapshot snapshot);
    }
}