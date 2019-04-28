using Bencode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bencode.DTOs;
using System.Net;

namespace BitTorrentEdu
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new BencodeParser();

            var torrentFactory = new TorrentFactory(parser);
            var torrent = torrentFactory.GetTorrentFromFile(@"G:\University\uzd2\debian-9.8.0-amd64-DVD-1.iso.torrent");

            var encoded = torrent.Info.GetUrlEncodedInfoHash();
        }
    }
}
