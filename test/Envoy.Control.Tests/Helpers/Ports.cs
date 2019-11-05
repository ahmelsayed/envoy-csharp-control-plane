using System.Net;
using System.Net.Sockets;

namespace Envoy.Control.Server.Tests
{
    public static class Ports
    {
        public static uint GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                return (uint)((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}