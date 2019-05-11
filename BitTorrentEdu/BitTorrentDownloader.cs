using Bencode;
using BitTorrentEdu.DTOs;
using Sockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Utils;

namespace BitTorrentEdu
{
    public class BitTorrentDownloader
    {
        public event EventHandler<PeerEventArgs> PeerEventHandlerEcho;

        private readonly object _pieceLock = new object();
        public List<TorrentPiece> Pieces {
            get {
                lock (_pieceLock)
                {
                    return _pieces.ToList();
                }
            }
        }
        public Dictionary<TorrentPiece, SocketPeer> PendingPieces {
            get {
                lock (_pieceLock)
                {
                    return _pendingPieces.ToDictionary(d => d.Key, d => d.Value);
                }
            }
        }

        public bool IsDownloadCompleted { get; set; } = false;

        private IByteConverter ByteConverter { get; set; } = new ByteConverter();

        private readonly Dictionary<TorrentPiece, SocketPeer> _pendingPieces = new Dictionary<TorrentPiece, SocketPeer>();
        private readonly List<TorrentPiece> _pieces = new List<TorrentPiece>();

        private readonly string _peerId;
        private readonly IBencodeParser _bencodeParser;
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly ITcpSocketHelper _tcpSocketHelper;
        private readonly ITorrentFactory _torrentFactory;
        private readonly ITrackerResponseFactory _trackerResponseFactory;
        private readonly IPeerEventDataFactory _peerEventDataFactory;

        public BitTorrentDownloader(string peerId, IBencodeParser bencodeParser, IHttpClientHelper httpClientHelper, ITcpSocketHelper tcpSocketHelper,
            ITorrentFactory torrentFactory, ITrackerResponseFactory trackerResponseFactory, IPeerEventDataFactory peerEventDataFactory)
        {
            _peerId = peerId;
            _bencodeParser = bencodeParser;
            _httpClientHelper = httpClientHelper;
            _tcpSocketHelper = tcpSocketHelper;
            _torrentFactory = torrentFactory;
            _trackerResponseFactory = trackerResponseFactory;
            _peerEventDataFactory = peerEventDataFactory;
        }

        public void DownloadTorrent(string torrentPath, string saveDir, int port)
        {
            var torrent = _torrentFactory.GetTorrentFromFile(torrentPath);
            var fileName = Path.GetFileNameWithoutExtension(torrent.Info.Name);
            string fullSaveDir = Path.Combine(saveDir, fileName);

            lock (_pieceLock)
            {
                foreach (var pieceIndex in Enumerable.Range(0, torrent.Info.PieceHashes.Count))
                {
                    var pieceLength = torrent.Info.PieceLength;
                    if (pieceIndex == torrent.Info.PieceHashes.Count - 1) //Last piece is odd length
                    {
                        var tempPieceLength = torrent.Info.Length % pieceLength;
                        if (tempPieceLength != 0)
                            pieceLength = tempPieceLength;
                    }

                    var piece = new TorrentPiece(pieceIndex, pieceLength, fullSaveDir);
                    _pieces.Add(piece);
                }
            }

            var tracker = new Tracker(_httpClientHelper, _bencodeParser, _trackerResponseFactory, _peerId, port); //Technically needs a factory for testing
            var peerConnector = new PeerConnector(_peerEventDataFactory, _tcpSocketHelper, _peerId, torrent.Info); //Technically needs a factory for testing

            while (GetNonCompletedPieces().Any())
            {
                try
                {
                    if (peerConnector.Peers.Count < 25)
                    {
                        var trackerResult = tracker.Track(torrent, TrackerEvent.Started).Result;
                        foreach (var peer in trackerResult.Peers)
                        {
                            var t = new Thread(() => peerConnector.TryConnectToPeer(peer, OnPeerEvent));
                            t.Start();
                        }
                    }

                    var neededPieces = GetAvailableForDownloadPieces();
                    foreach (var peer in peerConnector.Peers)
                    {
                        //Console.Write("{0, -40} {1, -30}", $"Connected to peer: {peer.Peer.Ip}:{peer.Peer.Port}", $"Expecting piece? {peer.AmWaitingForPiece}");
                        var neededPiecesIds = neededPieces
                            .Select(p => p.PieceIndex)
                            .ToList();

                        var nullablePieceIndex = peer.RetrievePieceIndexIfAny(neededPiecesIds);
                        if (nullablePieceIndex == null)
                        {
                            if (peer.AmInterested)
                                peer.SendInterest(false);

                            continue;
                        }

                        if (!peer.AmInterested || peer.PeerChocking)
                        {
                            peer.SendInterest(true);
                            continue;
                        }

                        if (!peer.PeerChocking && !peer.AmWaitingForPiece)
                        {
                            var pieceIndex = nullablePieceIndex.Value;
                            var neededPiece = neededPieces.Find(p => p.PieceIndex == pieceIndex);
                            neededPieces.Remove(neededPiece);

                            lock (_pieceLock)
                            {
                                _pendingPieces.Add(neededPiece, peer);
                                var block = neededPiece.GetNextBlock();
                                peer.RequestPiece((uint)pieceIndex, block.Offset, block.Length);
                            }
                            continue;
                        }

                        //peer.SendKeepAlive();
                        //Console.WriteLine("");
                    }

                    FreeBlockedPieces(peerConnector);
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"{ex.Message}");
                }
            }

            var fullFilePath = Path.Combine(fullSaveDir, torrent.Info.Name);
            CombinePiecesIntoFile(fullFilePath);

            CleanupResources(peerConnector);

            IsDownloadCompleted = true;
        }

        private void FreeBlockedPieces(IPeerConnector peerConnector)
        {
            lock (_pieceLock)
            {
                var peers = peerConnector.Peers;
                foreach (var pendingPiece in PendingPieces)
                {
                    var piece = pendingPiece.Key;
                    var peer = pendingPiece.Value;
                    if (!peers.Contains(peer))
                    {
                        _pendingPieces.Remove(piece);
                        continue;
                    }

                    if (peer.AmWaitingForPiece && (DateTime.Now - peer.RequestPieceTime.Value).TotalSeconds > Constants.PieceTimeout)
                        FreeUpPiece(peer);
                }
            }
        }

        private void CombinePiecesIntoFile(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                foreach (var piece in Pieces)
                {
                    var bytes = File.ReadAllBytes(piece.GetFullPath());
                    fileStream.Write(bytes, 0, bytes.Length);
                    File.Delete(piece.GetFullPath());
                }
            }
        }

        private void CleanupResources(IPeerConnector peerConnector)
        {
            foreach (var peer in peerConnector.Peers)
                peerConnector.DisconnectPeer(peer);
        }

        public List<TorrentPiece> GetAvailableForDownloadPieces()
        {
            lock (_pieceLock)
            {
                return Pieces
                    .Where(p => !IsCompleteOrPending(p))
                    .ToList();
            }
        }

        private bool IsCompleteOrPending(TorrentPiece piece)
        {
            return piece.Complete || PendingPieces.TryGetValue(piece, out var tempPeer);
        }

        public List<TorrentPiece> GetNonCompletedPieces()
        {
            lock (_pieceLock)
            {
                return Pieces
                    .Where(p => !p.Complete)
                    .ToList();
            }
        }

        private void OnPeerEvent(object sender, PeerEventArgs eventArgs)
        {
            PeerEventHandlerEcho?.Invoke(sender, eventArgs);

            var eventData = eventArgs.EventData;
            var senderPeer = (SocketPeer)sender;

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

                if (pieceIndex >= Pieces.Count)
                    return; //Got sent a piece with index that does not exist. Discarded.

                //TODO: CASTING TO INT HERE IS BAD. FIX.
                lock (_pieceLock)
                {
                    if (!_pieces[(int)pieceIndex].TryAddBlock(blockData, blockOffset))
                        return; //Add failed. Discarded.

                    _pendingPieces.Remove(_pieces[(int)pieceIndex]);
                }
            }
        }

        private void FreeUpPiece(SocketPeer peer)
        {
            lock (_pieceLock)
            {
                var piecePeers = _pendingPieces.Where(p => p.Value == peer);
                if (piecePeers.Any())
                {
                    var piecePeerPair = piecePeers.First();
                    _pendingPieces.Remove(piecePeerPair.Key);

                    peer.PieceRequestComplete();
                }
            }
        }
    }
}
