using System.Net;
using System.Net.Sockets;

namespace Sockets
{
    public interface ITcpSocketHelper
    {
        bool TryEstablishConnection(IPAddress ipAddress, int port, out Socket socket);
    }
}