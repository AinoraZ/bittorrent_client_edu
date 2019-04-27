using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bencode.DTOs
{
    public class Torrent
    {
        public TorrentInfoSingle Info { get; set; }
        public string AnounceUrl { get; set; }
        public int Uploaded { get; set; }
        public int Downloaded { get; set; }
        public int Left { get; set; }
    }
}
