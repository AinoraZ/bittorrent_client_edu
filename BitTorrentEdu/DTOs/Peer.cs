using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentEdu.DTOs
{
   public class Peer
    {
        public bool BinaryMode { get; set; } = false;
        public string Id { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
    }
}
