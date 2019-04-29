using Bencode;
using Sockets;
using System;

namespace BitTorrentEdu
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new BencodeParser();

            var torrentFactory = new TorrentFactory(parser);
            var torrent = torrentFactory.GetTorrentFromFile(@"G:\University\uzd2\03fd3cba845a8d252d9768806486f004d7f4e374.torrent");

            var httpClient = new HttpClientHelper();
            var trackerResponseFactory = new TrackerResponseFactory(parser);
            var peerId = "-ZA0001-000000000001";
            var tracker = new Tracker(httpClient, parser, trackerResponseFactory, peerId, 6881);

            var headTrackerResult = tracker.Track(torrent, TrackerEvent.Started).Result;

            var tcpSocketHelper = new TcpSocketHelper();
            var peerConnector = new PeerConnector(tcpSocketHelper, torrent.Info.InfoHash, peerId);

            foreach (var peer in headTrackerResult.Peers)
            {
                if (peerConnector.TryConnectToPeer(peer))
                    Console.WriteLine($"Connected to peer {peer.Ip}:{peer.Port}");
            }
        }
    }
}
