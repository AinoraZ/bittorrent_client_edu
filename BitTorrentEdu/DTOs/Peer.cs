using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentEdu.DTOs
{
   public class Peer
    {
        public Peer(IPAddress ip, long port, string id = null)
        {
            Id = id;
            Ip = ip;
            Port = port;
        }

        public string Id { get; private set; }
        public IPAddress Ip { get; private set; }
        public long Port { get; private set; }
    }
}
