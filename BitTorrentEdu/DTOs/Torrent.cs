using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentEdu.DTOs
{
    public class Torrent
    {
        public Torrent(string announceUrl, TorrentInfoSingle info)
        {
            AnnounceUrl = announceUrl;
            Info = info;
            Left = info.Length;
        }

        public TorrentInfoSingle Info { get; set; }
        public string AnnounceUrl { get; set; }
        public long Uploaded { get; set; }
        public long Downloaded { get; set; }
        public long Left { get; set; }
    }
}
