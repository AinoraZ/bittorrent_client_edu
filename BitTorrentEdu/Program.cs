using Bencode;
using BitTorrentEdu.DTOs;
using Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Utils;

namespace BitTorrentEdu
{
    class Program
    {
        static List<TorrentPiece> Pieces { get; set; } = new List<TorrentPiece>();
        static object pieceLock = new object();
        static IByteConverter ByteConverter { get; set; } = new ByteConverter();

        static void Main(string[] args)
        {
            var parser = new BencodeParser();

            var torrentFactory = new TorrentFactory(parser);
            var torrent = torrentFactory.GetTorrentFromFile(@"G:\University\uzd2\somePdf.torrent");

            string saveDir = @"G:\University\uzd2\Downloads\";

            foreach (var pieceIndex in Enumerable.Range(0, torrent.Info.PieceHashes.Count))
            {
                var pieceLength = torrent.Info.PieceLength;
                if (pieceIndex == torrent.Info.PieceHashes.Count - 1)
                    pieceLength = torrent.Info.Length % pieceLength;

                var piece = new TorrentPiece(pieceIndex, pieceLength, saveDir);
                Pieces.Add(piece);
            }

            var httpClient = new HttpClientHelper();
            var trackerResponseFactory = new TrackerResponseFactory(parser);
            var peerId = "-ZA0001-000000000001";
            var tracker = new Tracker(httpClient, parser, trackerResponseFactory, peerId, 6881);

            //var headTrackerResult = tracker.Track(torrent, TrackerEvent.Started).Result;

            var tcpSocketHelper = new TcpSocketHelper();
            var peerEventDataFactory = new PeerEventDataFactory();
            var peerConnector = new PeerConnector(peerEventDataFactory, tcpSocketHelper, peerId, torrent.Info);

            if (peerConnector.Peers.Count < 30)
            {
                var trackerResult = tracker.Track(torrent, TrackerEvent.Started).Result;
                foreach (var peer in trackerResult.Peers)
                {
                    var t = new Thread(() => peerConnector.TryConnectToPeer(peer, OnPeerEvent));
                    t.Start();
                }
            }
            while (GetNonCompletedPieces().Any())
            {
                Thread.Sleep(100);
                var neededPieces = GetAvailableForDownloadPieces();

                foreach (var peer in peerConnector.Peers)
                {
                    Console.Write("{0, -40} {1, -30}", $"Connected to peer: {peer.Peer.Ip}:{peer.Peer.Port}", $"Expecting piece? {peer.AmWaitingForPiece}");
                    try
                    {
                        var neededPiecesIds = neededPieces
                            .Select(p => p.PieceIndex)
                            .ToList();

                        var nullablePieceIndex = peer.RetrievePieceIndexIfAny(neededPiecesIds);
                        if (nullablePieceIndex == null)
                        {
                            if (peer.AmInterested)
                                peer.SendInterest(false);

                            Console.WriteLine();
                            continue;
                        }

                        if (!peer.AmInterested || peer.PeerChocking)
                        {
                            Console.WriteLine($"Sending interest");
                            peer.SendInterest(true);
                            continue;
                        }

                        if (!peer.PeerChocking && !peer.AmWaitingForPiece)
                        {
                            var pieceIndex = nullablePieceIndex.Value;
                            var neededPiece = neededPieces.Find(p => p.PieceIndex == pieceIndex);
                            neededPieces.Remove(neededPiece);
                            neededPiece.Pending = true;

                            var block = neededPiece.GetNextBlock();

                            Console.WriteLine($"Asking for piece: {pieceIndex}");
                            peer.RequestPiece((uint) pieceIndex, block.Offset, block.Length);
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

            var fullFilePath = Path.Combine(saveDir, torrent.Info.Name);
            using (var fileStream = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write))
            {
                foreach (var piece in Pieces)
                {
                    var bytes = File.ReadAllBytes(piece.GetFullPath());
                    fileStream.Write(bytes, 0, bytes.Length);
                }
            }
        }

        public static List<TorrentPiece> GetAvailableForDownloadPieces()
        {
            lock (pieceLock)
            {
                return Pieces
                    .Where(p => !p.IsCompletedOrPending())
                    .ToList();
            }
        }

        public static List<TorrentPiece> GetNonCompletedPieces()
        {
            lock (pieceLock)
            {
                return Pieces
                    .Where(p => !p.Complete)
                    .ToList();
            }
        }

        public static void OnPeerEvent(object sender, PeerEventArgs eventArgs)
        {
            var eventData = eventArgs.EventData;
            var senderPeer = (SocketPeer)sender;

            Console.WriteLine($"Peer {senderPeer.Peer.Ip}: EVENT {eventData.EventType}");

            if (eventData.EventType == PeerEventType.Piece)
            {
                if (eventData.Length < 9)
                    return; //Payload length must be at least 9. Discarded otherwise.

                var payload = eventData.Payload;
                var pieceIndex = ByteConverter.BytesToUint(payload.Take(4).ToArray());
                var blockOffset = ByteConverter.BytesToUint(payload.Skip(4).Take(4).ToArray());
                var blockData = payload.Skip(8).ToArray();

                if (pieceIndex > Pieces.Count - 1)
                    return; //Got sent a piece with index that does not exist. Discarded.

                //TODO: CASTING TO INT HERE IS BAD. FIX.
                if (!Pieces[(int)pieceIndex].TryAddBlock(blockData, blockOffset))
                    return; //Add failed. Discarded.

                //Console.WriteLine($"Event {eventData.EventType}: Peer {senderPeer.Peer.Ip}: GOT PIECE!!!!");
            }
        }
    }
}
