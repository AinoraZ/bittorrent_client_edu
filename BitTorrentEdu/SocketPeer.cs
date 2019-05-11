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
        public DateTime? RequestPieceTime { get; private set; } = null;
        public bool AmWaitingForPiece {
            get 
            {
                return RequestPieceTime != null;
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
            if (!neededPieces.Any())
                return null;

            if (!PieceIndexes.Any())
                return null;

            var intersection = PieceIndexes.Intersect(neededPieces).ToList();
            if (!intersection.Any())
                return null;

            return intersection.First();
        }

        public bool TryInitiateHandsake()
        {
            var peerHandshake = new PeerHandshake(Constants.HandshakeProtocolIdentifier, InfoHash, PeerId);

            try
            {
                Socket.Send(peerHandshake.ToHandshakeBytes());
                byte[] response = new byte[Constants.MaxMessageSize];
                var bytesReceived = Socket.Receive(response);

                if(bytesReceived == 0)
                    return false;

                var handshakeContent = new PeerHandshake(response.Take(bytesReceived).ToArray());

                if (!InfoHash.SequenceEqual(handshakeContent.InfoHash))
                    return false;

                var totalHandshakeBytes = handshakeContent.Length + handshakeContent.Reserved.Length + 1 +
                    handshakeContent.InfoHash.Length + handshakeContent.PeerId.Length;

                var leftovers = response.Skip(totalHandshakeBytes).Take(bytesReceived - totalHandshakeBytes);
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

        private void ReadLoop()
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

                if (bytesReceived == 0)
                {
                    HandleDisconnect("0 bytes read. Disconnected");
                    return;
                }

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
                HandleDisconnect(ex.Message);
            }
        }

        private void HandleDisconnect(string exceptionMessage = null)
        {
            var peerEventData = new PeerEventData(PeerEventStatus.Error, PeerEventType.ConnectionClosed, 0, null, exceptionMessage);
            var eventArgs = new PeerEventArgs(peerEventData);
            PeerEventHandler(this, eventArgs);
        }

        private void HandlePeerEvent (PeerEventData peerEventData)
        {
            if (peerEventData.EventStatus == PeerEventStatus.Partial)
                return;

            if (peerEventData.EventType == PeerEventType.Choke)
                PeerChocking = true;
            if (peerEventData.EventType == PeerEventType.Unchoke)
                PeerChocking = false;
            if (peerEventData.EventType == PeerEventType.Piece)
                PieceRequestComplete();
            if (peerEventData.EventType == PeerEventType.Bitfield && !ParseBitfield(peerEventData))
            {
                HandleDisconnect("Invalid bitfield sent");
                return;
            }

            var eventArgs = new PeerEventArgs(peerEventData);
            PeerEventHandler(this, eventArgs);
        }

        private bool ParseBitfield(PeerEventData peerEventData)
        {
            var byteBitsList = peerEventData.Payload.Select(b => new BitArray(new byte[] { b }));

            if (byteBitsList.Count() * 8 < PieceAmount - 1)
                return false;

            long pieceIndex = 0;
            foreach (var byteBits in byteBitsList)
            {
                for (int byteIndex = 7; byteIndex >= 0; byteIndex--)
                {
                    if (pieceIndex >= PieceAmount && byteBits[byteIndex]) //Extra bits must be 0
                        return false;

                    if (byteBits[byteIndex])
                        PieceIndexes.Add(pieceIndex);

                    pieceIndex++;
                }
            }

            return true;
        }

        private void ParseHave(PeerEventData peerEventData)
        {

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
            RequestPieceTime = DateTime.Now;
            var sendContent = new List<byte> { 0, 0, 0, 0x0d, 6 };

            sendContent.AddRange(ByteConverter.UIntToBytes(pieceIndex));
            sendContent.AddRange(ByteConverter.UIntToBytes(blockBeginIndex));
            sendContent.AddRange(ByteConverter.UIntToBytes(blockLength));

            Socket.Send(sendContent.ToArray());
        }

        public void PieceRequestComplete()
        {
            RequestPieceTime = null;
        }

        public void Dispose()
        {
            EndReceive();
            Socket.Close();
        }
    }
}
