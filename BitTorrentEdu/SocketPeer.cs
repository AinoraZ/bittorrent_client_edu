using BitTorrentEdu.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Utils;

namespace BitTorrentEdu
{
    public class SocketPeer : IDisposable
    {
        public event EventHandler<PeerEventArgs> PeerEventHandler;

        public Peer Peer { get; }
        private List<long> PieceIndexes { get; } = new List<long>();
        public bool AmInterested { get; private set; } = false;
        public bool PeerChocking { get; private set; } = true;
        public long? RequestedPiece { get; private set; } = null;
        public DateTime? RequestPieceTime { get; private set; } = null;
        public bool AmWaitingForPiece {
            get 
            {
                return RequestedPiece != null;
            }
        }

        private Socket Socket { get; }
        private byte[] InfoHash { get; }
        private string PeerId { get; }
        private long PieceLength { get; }
        private long PieceAmount { get; }

        private Thread ReceiveThread { get; set; } = null;
        private bool ThreadRunning { get; set; } = true;
        private List<byte> TemporaryReceiveBuffer { get; set; } = new List<byte>(); 

        private IPeerEventDataFactory PeerEventDataFactory { get; set; } = new PeerEventDataFactory();
        public IByteConverter ByteConverter { get; set; } = new ByteConverter();

        public SocketPeer(IPeerEventDataFactory peerEventDataFactory, Peer peer, Socket socket, TorrentInfoSingle torrentInfo, string peerId)
        {
            if (torrentInfo.InfoHash.Length != Constants.InfoHashLength)
                throw new ArgumentException($"Info hash must be {Constants.InfoHashLength} bytes");

            if (peerId.Length != Constants.PeerIdLength)
                throw new ArgumentException($"Peer id must be {Constants.PeerIdLength} bytes");

            PeerEventDataFactory = peerEventDataFactory;
            Peer = peer;
            Socket = socket;
            PeerId = peerId;
            InfoHash = torrentInfo.InfoHash;
            PieceLength = torrentInfo.PieceLength;
            PieceAmount = torrentInfo.PieceHashes.Count;
        }

        public long? RetrievePieceIndexIfAny(List<long> neededPieces)
        {
            try
            {
                return PieceIndexes.Intersect(neededPieces).First();
            }
            catch
            {
                return null;
            }
        }

        public bool TryInitiateHandsake()
        {
            var peerHandshake = new PeerHandshake(Constants.HandshakeProtocolIdentifier, InfoHash, PeerId);

            try
            {
                Socket.Send(peerHandshake.ToHandshakeBytes());
                byte[] response = new byte[Constants.MaxMessageSize];
                var bytesRead = Socket.Receive(response);

                var handshakeContent = new PeerHandshake(response.Take(bytesRead).ToArray());

                if (!InfoHash.SequenceEqual(handshakeContent.InfoHash))
                    return false;

                var totalHandshakeBytes = handshakeContent.Length + handshakeContent.Reserved.Length + 1 +
                    handshakeContent.InfoHash.Length + handshakeContent.PeerId.Length;

                var leftovers = response.Skip(totalHandshakeBytes).Take(bytesRead - totalHandshakeBytes);
                TemporaryReceiveBuffer.AddRange(leftovers);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void StartReceive()
        {
            if (ReceiveThread != null)
                return;

            ReceiveThread = new Thread(ReadLoop);
            ReceiveThread.Start();
        }

        public void EndReceive()
        {
            ThreadRunning = false;
            ReceiveThread = null;
        }

        public void ReadLoop()
        {
            while (ThreadRunning)
            {
                var listenList = new List<Socket> { Socket };
                Socket.Select(listenList, null, null, 1000);

                if (!ThreadRunning) break;

                if (listenList.Contains(Socket))
                    RecieveResponse();
            }
        }

        public void RecieveResponse()
        {
            try
            {
                byte[] response = new byte[Constants.MaxMessageSize];
                var bytesReceived = Socket.Receive(response);

                var trimmedResponse = response.Take(bytesReceived);
                TemporaryReceiveBuffer.AddRange(trimmedResponse);

                PeerEventData peerEventData;
                do
                {
                    var peerEventDataWrapper = PeerEventDataFactory.TryParsePeerEventDataFromEnumerable(TemporaryReceiveBuffer);
                    TemporaryReceiveBuffer = peerEventDataWrapper.UnusedBytes.ToList(); //Store unused bytes for later use

                    peerEventData = peerEventDataWrapper.EventData;
                    HandlePeerEvent(peerEventData);
                }
                while (TemporaryReceiveBuffer.Count != 0 && peerEventData.EventStatus == PeerEventStatus.Ok);
            }
            catch (Exception ex)
            {
                var peerEventData = new PeerEventData(PeerEventStatus.Error, PeerEventType.ConnectionClosed, 0, null, ex.Message);
                var eventArgs = new PeerEventArgs(peerEventData);
                PeerEventHandler(this, eventArgs);
            }
        }

        public void HandlePeerEvent (PeerEventData peerEventData)
        {
            if (peerEventData.EventStatus == PeerEventStatus.Partial)
                return;

            if (peerEventData.EventType == PeerEventType.Choke)
                PeerChocking = true;
            if (peerEventData.EventType == PeerEventType.Unchoke)
                PeerChocking = false;
            if (peerEventData.EventType == PeerEventType.Piece)
                PieceRequestComplete();
            if (peerEventData.EventType == PeerEventType.Bitfield)
                ParseBitfield(peerEventData);

            var eventArgs = new PeerEventArgs(peerEventData);
            PeerEventHandler(this, eventArgs);
        }

        public void ParseBitfield(PeerEventData peerEventData)
        {
            var byteBitsList = peerEventData.Payload.Select(b => new BitArray(new byte[] { b }));

            long pieceIndex = 0;
            foreach (var byteBits in byteBitsList)
            {
                for (int byteIndex = 7; byteIndex >= 0; byteIndex--)
                {
                    if (pieceIndex >= PieceAmount)
                        break;

                    if (byteBits[byteIndex])
                        PieceIndexes.Add(pieceIndex);

                    pieceIndex++;
                }
            }

            if (pieceIndex < PieceAmount - 1)
            {
                //Bad bitfield
            }
        }

        public void SendKeepAlive()
        {
            var sendContent = new byte[] { 0, 0, 0, 0 };
            Socket.Send(sendContent);
        }

        public void SendInterest(bool areInterested)
        {
            AmInterested = areInterested;
            var sendContent = new byte[] { 0, 0, 0, 1, areInterested ? (byte)2 : (byte)3 };
            Socket.Send(sendContent);
        }

        public void RequestPiece(uint pieceIndex, uint blockBeginIndex, uint blockLength)
        {
            RequestedPiece = pieceIndex;
            RequestPieceTime = DateTime.Now;
            var sendContent = new List<byte> { 0, 0, 0, 0x0d, 6 };

            sendContent.AddRange(ByteConverter.UIntToBytes(pieceIndex));
            sendContent.AddRange(ByteConverter.UIntToBytes(blockBeginIndex));
            sendContent.AddRange(ByteConverter.UIntToBytes(blockLength));

            Socket.Send(sendContent.ToArray());
        }

        public void PieceRequestComplete()
        {
            RequestedPiece = null;
            RequestPieceTime = null;
        }

        public void Dispose()
        {
            EndReceive();
            Socket.Close();
        }
    }
}
