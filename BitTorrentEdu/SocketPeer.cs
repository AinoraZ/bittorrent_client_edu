using BitTorrentEdu.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace BitTorrentEdu
{
    public class SocketPeer : IDisposable
    {
        //TODO: Implament an event to listen to when peer disconnects (will be consumed by PeerConnector)

        public event EventHandler<PeerEventArgs> PeerEventHandler;

        public Peer Peer { get; }
        public Socket Socket { get; }
        public byte[] InfoHash { get; }
        public string PeerId { get; }
        public bool AmInterested { get; private set; } = false;
        public bool PeerChocking { get; private set; } = true;
        public bool AmWaitingForPiece { get; private set; } = false;

        public List<long> PieceIndexes { get; } = new List<long>();

        private Thread ReceiveThread { get; set; } = null;
        private bool ThreadRunning { get; set; } = true;

        private List<byte> TemporaryReceiveBuffer { get; set; } = new List<byte>(); 

        public PeerEventDataFactory PeerEventDataFactory { get; set; } = new PeerEventDataFactory();

        public SocketPeer(Peer peer, Socket socket, byte[] infoHash, string peerId)
        {
            if (infoHash.Length != Constants.InfoHashLength)
                throw new ArgumentException($"Info hash must be {Constants.InfoHashLength} bytes");

            if (peerId.Length != Constants.PeerIdLength)
                throw new ArgumentException($"Peer id must be {Constants.PeerIdLength} bytes");

            Peer = peer;
            Socket = socket;
            InfoHash = infoHash;
            PeerId = peerId;
        }

        public bool TryInitiateHandsake()
        {
            var peerHandshake = new PeerHandshake(Constants.HandshakeProtocolIdentifier, InfoHash, PeerId);

            try
            {
                Socket.Send(peerHandshake.ToHandshakeBytes());
                byte[] response = new byte[(int) Math.Pow(2, 17)];
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

                Thread.Sleep(500);
            }
        }

        public void RecieveResponse()
        {
            try
            {
                byte[] response = new byte[(int)Math.Pow(2, 17)];
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
                var peerEventData = new PeerEventData(PeerEventStatus.Error, PeerEventType.ConnectionClosed, null, ex.Message);
                var eventArgs = new PeerEventArgs(peerEventData);
                PeerEventHandler(this, eventArgs);
            }
        }

        public void HandlePeerEvent (PeerEventData peerEventData)
        {
            if (peerEventData.EventStatus == PeerEventStatus.Partial)
            {
                Console.WriteLine($"Partial data encountered. Expected length: {peerEventData.Length}");
                return;
            }

            if (peerEventData.EventType == PeerEventType.Choke)
                PeerChocking = true;
            if (peerEventData.EventType == PeerEventType.Unchoke)
                PeerChocking = false;
            if (peerEventData.EventType == PeerEventType.Piece)
                AmWaitingForPiece = false;
            if (peerEventData.EventType == PeerEventType.Bitfield)
            {
                ParseBitfield(peerEventData);
            }

            var eventArgs = new PeerEventArgs(peerEventData);
            PeerEventHandler(this, eventArgs);
        }

        public void ParseBitfield(PeerEventData peerEventData)
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
            AmWaitingForPiece = true;
            var sendContent = new List<byte> { 0, 0, 0, 0x0d, 6 };

            sendContent.AddRange(UIntToBytes(pieceIndex));
            sendContent.AddRange(UIntToBytes(blockBeginIndex));
            sendContent.AddRange(UIntToBytes(blockLength));

            Socket.Send(sendContent.ToArray());
        }

        private byte[] UIntToBytes(uint value)
        {
            var byteValue = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(byteValue);

            return byteValue;
        }

        public void Dispose()
        {
            EndReceive();
            Socket.Close();
        }
    }
}
