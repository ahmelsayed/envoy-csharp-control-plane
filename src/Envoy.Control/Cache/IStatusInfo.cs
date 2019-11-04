namespace Envoy.Control.Cache
{
    public interface IStatusInfo<T>
    {
        long LastWatchRequestTime { get; }
        T NodeGroup { get; }
        int NumWatches { get; }
    }
}