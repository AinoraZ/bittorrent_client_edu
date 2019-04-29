using System.Net;

namespace BitTorrentEdu.DTOs
{
    public class Peer
    {
        public Peer(IPAddress ip, int port, string id = null)
        {
            Id = id;
            Ip = ip;
            Port = port;
        }

        public string Id { get; private set; }
        public IPAddress Ip { get; private set; }
        public int Port { get; private set; }
    }
}
