using BitTorrentEdu.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        private Thread ReceiveThread { get; set; } = null;
        private bool ThreadRunning { get; set; } = true;

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
                byte[] response = new byte[256];
                var bytesRead = Socket.Receive(response);

                var handshakeContent = new PeerHandshake(response.Take(bytesRead).ToArray());

                if (!InfoHash.SequenceEqual(handshakeContent.InfoHash))
                    return false;

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
            ReceiveThread?.Interrupt();
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

                Thread.Sleep(1000);
            }
        }

        public async Task SendKeepAliveAsync()
        {
            await Task.Delay(1);
            throw new NotImplementedException();
        }

        public async Task SendInterestAsync(bool areInterested)
        {
            AmInterested = areInterested;
            await Task.Delay(1);
            throw new NotImplementedException();
        }

        public async Task RequestPieceAsync(int pieceIndex, int blockBeginIndex, int blockLength)
        {
            await Task.Delay(1);
            throw new NotImplementedException();
        }

        public void RecieveResponse()
        {
            try
            {
                byte[] response = new byte[(int)Math.Pow(2, 17)];
                var bytesReceived = Socket.Receive(response);

                var trimmedResponse = response.Take(bytesReceived).ToArray();
                var peerEvents = new List<PeerEventData>();
                int totalBytesRead = 0;
                var peerEventData = PeerEventDataFactory.GeneratePeerEventDataFromByteArray(trimmedResponse.Skip(totalBytesRead).ToArray());

                if (peerEventData.EventStatus == PeerEventStatus.Error)
                {
                    Console.WriteLine($"Peer {Peer.Ip}: Unsupported client encountered");
                }

                if (peerEventData.EventStatus == PeerEventStatus.Partial)
                {
                    Console.WriteLine($"Event {peerEventData.EventType}: Peer {Peer.Ip}: Waiting for more data");
                }

                if (peerEventData.EventType == PeerEventType.Choke)
                    PeerChocking = true;
                if (peerEventData.EventType == PeerEventType.Unchoke)
                    PeerChocking = false;

                var eventArgs = new PeerEventArgs(peerEventData);
                PeerEventHandler(this, eventArgs);

            }
            catch (Exception ex)
            {
                //TODO: Change with normal logging
                Console.WriteLine(ex.Message);
            }

        }

        public void Dispose()
        {
            EndReceive();
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();
        }
    }
}
