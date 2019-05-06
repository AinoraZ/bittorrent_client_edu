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
            if (args.Length != 3)
            {
                Console.Error.WriteLine($"You must pass 3 arguments: Path to torrent, Download directory, Port (Range: [{Constants.MinPortNumber}:{Constants.MaxPortNumber}])");
                return;
            }

            var torrentPath = args[0];
            if (!File.Exists(torrentPath))
            {
                Console.Error.WriteLine("Passed torrent does not exist");
                return;
            }

            var partialSaveDir = args[1];
            if (!int.TryParse(args[2], out int port))
            {
                Console.Error.WriteLine("Invalid port int passed");
                return;
            }

            if (port > Constants.MaxPortNumber || port < Constants.MinPortNumber)
            {
                Console.Error.WriteLine($"Port must be in range: [{Constants.MinPortNumber}:{Constants.MaxPortNumber}]");
                return;
            }

            var parser = new BencodeParser();

            var torrentFactory = new TorrentFactory(parser);
            var torrent = torrentFactory.GetTorrentFromFile(torrentPath);

            var fileName = Path.GetFileNameWithoutExtension(torrent.Info.Name);
            string saveDir = Path.Combine(partialSaveDir, fileName);

            foreach (var pieceIndex in Enumerable.Range(0, torrent.Info.PieceHashes.Count))
            {
                var pieceLength = torrent.Info.PieceLength;
                if (pieceIndex == torrent.Info.PieceHashes.Count - 1) //Last piece is odd length
                    pieceLength = torrent.Info.Length % pieceLength;

                var piece = new TorrentPiece(pieceIndex, pieceLength, saveDir);
                Pieces.Add(piece);
            }

            var httpClient = new HttpClientHelper();
            var trackerResponseFactory = new TrackerResponseFactory(parser);
            var peerId = "-ZA0001-000000000001";
            var tracker = new Tracker(httpClient, parser, trackerResponseFactory, peerId, port);

            //var headTrackerResult = tracker.Track(torrent, TrackerEvent.Started).Result;

            var tcpSocketHelper = new TcpSocketHelper();
            var peerEventDataFactory = new PeerEventDataFactory();
            var peerConnector = new PeerConnector(peerEventDataFactory, tcpSocketHelper, peerId, torrent.Info);

            Console.Write("Downloading file... ");
            using (var progress = new ProgressBar(40))
            {
                while (GetNonCompletedPieces().Any())
                {
                    var trackerResult = tracker.Track(torrent, TrackerEvent.Started).Result;
                    foreach (var peer in trackerResult.Peers)
                    {
                        var t = new Thread(() => peerConnector.TryConnectToPeer(peer, OnPeerEvent));
                        t.Start();
                    }

                    var completedPieces = Pieces.Count - GetNonCompletedPieces().Count;
                    progress.Report((double) completedPieces / Pieces.Count);

                    Thread.Sleep(Constants.UpdateClockMs);
                    var neededPieces = GetAvailableForDownloadPieces();

                    foreach (var peer in peerConnector.Peers)
                    {
                        //Console.Write("{0, -40} {1, -30}", $"Connected to peer: {peer.Peer.Ip}:{peer.Peer.Port}", $"Expecting piece? {peer.AmWaitingForPiece}");
                        try
                        {
                            if (peer.AmWaitingForPiece && (DateTime.Now - peer.RequestPieceTime.Value).TotalSeconds > Constants.PieceTimeout)
                                FreeUpPiece(peer);

                            var neededPiecesIds = neededPieces
                                .Select(p => p.PieceIndex)
                                .ToList();

                            var nullablePieceIndex = peer.RetrievePieceIndexIfAny(neededPiecesIds);
                            if (nullablePieceIndex == null)
                            {
                                if (peer.AmInterested)
                                    peer.SendInterest(false);

                                //Console.WriteLine();
                                continue;
                            }

                            if (!peer.AmInterested || peer.PeerChocking)
                            {
                                //Console.WriteLine($"Sending interest");
                                peer.SendInterest(true);
                                continue;
                            }

                            if (!peer.PeerChocking && !peer.AmWaitingForPiece)
                            {
                                var pieceIndex = nullablePieceIndex.Value;
                                var neededPiece = neededPieces.Find(p => p.PieceIndex == pieceIndex);
                                neededPieces.Remove(neededPiece);
                                neededPiece.Pending = true;

                                lock (pieceLock)
                                {
                                    var block = neededPiece.GetNextBlock();
                                    //Console.WriteLine($"Asking for piece: {pieceIndex}");
                                    peer.RequestPiece((uint) pieceIndex, block.Offset, block.Length);
                                }
                                continue;
                            }

                            //peer.SendKeepAlive();
                            //Console.WriteLine("");
                        }
                        catch (Exception ex)
                        {
                            //Console.WriteLine($"{ex.Message}");
                        }
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
                    File.Delete(piece.GetFullPath());
                }
            }

            Console.WriteLine("Download done. Cleaning up...");
            foreach (var peer in peerConnector.Peers)
            {
                peerConnector.DisconnectPeer(peer);
            }
            Console.WriteLine("Done cleaning up. Press any key to exit...");
            Console.ReadLine();

            return;
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

            //Console.WriteLine($"Peer {senderPeer.Peer.Ip}: EVENT {eventData.EventType}");

            if (eventData.EventType == PeerEventType.Choke || eventData.EventType == PeerEventType.ConnectionClosed)
            {
                FreeUpPiece(senderPeer);
            }

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
                lock (pieceLock)
                {
                    if (!Pieces[(int)pieceIndex].TryAddBlock(blockData, blockOffset))
                        return; //Add failed. Discarded.
                }
            }
        }

        public static void FreeUpPiece(SocketPeer peer)
        {
            lock (pieceLock)
            {
                var pieceIndex = peer.RequestedPiece;
                if (pieceIndex != null)
                {
                    Pieces[(int)pieceIndex].Pending = false;
                    peer.PieceRequestComplete();
                }
            }
        }
    }
}
