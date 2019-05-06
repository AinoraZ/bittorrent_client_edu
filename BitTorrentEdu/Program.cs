using Bencode;
using BitTorrentEdu.DTOs;
using Sockets;
using System;
using System.Threading;

namespace BitTorrentEdu
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new BencodeParser();

            var torrentFactory = new TorrentFactory(parser);
            var torrent = torrentFactory.GetTorrentFromFile(@"G:\University\uzd2\somePdf.torrent");

            var httpClient = new HttpClientHelper();
            var trackerResponseFactory = new TrackerResponseFactory(parser);
            var peerId = "-ZA0001-000000000001";
            var tracker = new Tracker(httpClient, parser, trackerResponseFactory, peerId, 6881);

            //var headTrackerResult = tracker.Track(torrent, TrackerEvent.Started).Result;

            var tcpSocketHelper = new TcpSocketHelper();
            var peerConnector = new PeerConnector(tcpSocketHelper, peerId, torrent.Info);

            if (peerConnector.Peers.Count < 30)
            {
                var trackerResult = tracker.Track(torrent, TrackerEvent.Started).Result;
                foreach (var peer in trackerResult.Peers)
                {
                    var t = new Thread(() => peerConnector.TryConnectToPeer(peer));
                    t.Start();
                }
            }
            while (true)
            {

                Thread.Sleep(10000);

                Random random = new Random();

                foreach (var peer in peerConnector.Peers)
                {
                    Console.Write("{0, -40} {1, -30}", $"Connected to peer: {peer.Peer.Ip}:{peer.Peer.Port}", $"Expecting piece? {peer.AmWaitingForPiece}");
                    try
                    {
                        if (!peer.AmInterested || peer.PeerChocking)
                        {
                            Console.WriteLine($"Sending interest");
                            peer.SendInterest(true);
                            continue;
                        }

                        if (!peer.PeerChocking && !peer.AmWaitingForPiece)
                        {
                            var pieceIndex = (uint) random.Next(0, torrent.Info.PieceHashes.Count);
                            Console.WriteLine($"Asking for piece: {pieceIndex}");
                            peer.RequestPiece(pieceIndex, 0, (uint)Math.Pow(2, 14));
                            continue;
                        }

                        //peer.SendKeepAlive();
                        Console.WriteLine("");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex.Message}");
                    }
                }
            }
        }

        public static void OnPeerEvent(object sender, PeerEventArgs eventArgs)
        {
            var eventData = eventArgs.EventData;
            var senderPeer = (SocketPeer)sender;

            Console.WriteLine($"Peer {senderPeer.Peer.Ip}: EVENT {eventData.EventType}");

            if (eventData.EventType == PeerEventType.Piece)
            {
                //Console.WriteLine($"Event {eventData.EventType}: Peer {senderPeer.Peer.Ip}: GOT PIECE!!!!");
            }
            if (eventData.EventType == PeerEventType.Bitfield)
            {
                //Console.WriteLine($"Event {eventData.EventType}: Peer {senderPeer.Peer.Ip}: GOT BITFIELD!!!!");
            }
        }
    }
}
