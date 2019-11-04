using Envoy.Api.V2.Core;

namespace Envoy.Control.Cache
{
    public interface INodeGroup<T>
    {
        T hash(Node node);
    }
}