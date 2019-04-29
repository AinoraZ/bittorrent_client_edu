using Bencode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bencode.DTOs;
using System.Net;
using Sockets;

namespace BitTorrentEdu
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new BencodeParser();

            var torrentFactory = new TorrentFactory(parser);
            var torrent = torrentFactory.GetTorrentFromFile(@"G:\University\uzd2\debian-9.8.0-amd64-DVD-1.iso.torrent");

            var httpClient = new HttpClientHelper();
            var trackerResponseFactory = new TrackerResponseFactory(parser);
            var tracker = new Tracker(httpClient, parser, trackerResponseFactory, "-ZA0001-000000000001", 6881);

            tracker.Track(torrent, TrackerEvent.Started).Wait();
        }
    }
}
