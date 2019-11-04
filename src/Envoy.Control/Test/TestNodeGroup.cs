using Envoy.Api.V2.Core;
using Envoy.Control.Cache;

namespace Test
{
    public class TestNodeGroup : INodeGroup<string>
    {
        private string _key;

        public TestNodeGroup(string key)
        {
            this._key = key;
        }

        public string hash(Node node)
        {
            return this._key;
        }
    }
}