using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace Sockets
{
    public class TcpSocketHelper : ITcpSocketHelper
    {
        public bool TryEstablishConnection(IPAddress ipAddress, int port, out Socket socket)
        {
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);  
            socket = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(remoteEP);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
