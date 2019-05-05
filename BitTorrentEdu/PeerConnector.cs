using BitTorrentEdu.DTOs;
using Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentEdu
{
    public class PeerConnector : IPeerConnector
    {
        public List<SocketPeer> Peers { get; private set; } = new List<SocketPeer>();
        public ITcpSocketHelper TcpSocketHelper { get; private set; }
        public byte[] InfoHash { get; }
        public string PeerId { get; }

        private List<Peer> PendingPeers { get; set; } = new List<Peer>();

        private readonly object peerLock = new object();

        public PeerConnector(ITcpSocketHelper tcpSocketHelper, byte[] infoHash, string peerId)
        {
            if (infoHash.Length != Constants.InfoHashLength)
                throw new ArgumentException($"Info hash must be {Constants.InfoHashLength} bytes");

            if (peerId.Length != Constants.PeerIdLength)
                throw new ArgumentException($"Peer id must be {Constants.PeerIdLength} bytes");

            TcpSocketHelper = tcpSocketHelper;
            InfoHash = infoHash;
            PeerId = peerId;
        }

        public bool IsPeerConnected (Peer peer)
        {
            return Peers.Any(p => p.Peer.Ip == peer.Ip) || PendingPeers.Any(p => p.Ip == peer.Ip);
        }

        public bool TryConnectToPeer (Peer peer)
        {
            lock (peerLock)
            {
                if (IsPeerConnected(peer))
                    return false;

                PendingPeers.Add(peer);
            }

            if (!TcpSocketHelper.TryEstablishConnection(peer.Ip, peer.Port, out Socket socket))
            {
                lock (peerLock)
                {
                    PendingPeers.Remove(peer);
                    return false;
                }
            }

            var socketPeer = new SocketPeer(peer, socket, InfoHash, PeerId);
            if (!socketPeer.TryInitiateHandsake())
            {
                lock (peerLock)
                {
                    PendingPeers.Remove(peer);
                }
                socketPeer.Dispose();
                return false;
            }

            socketPeer.PeerEventHandler += OnPeerEvent;
            lock(peerLock)
            {
                Peers.Add(socketPeer);
                PendingPeers.Remove(peer);
            }
            Console.WriteLine($"Connected to peer {peer.Ip}:{peer.Port}");

            socketPeer.StartReceive();

            return true;
        }

        public void OnPeerEvent(object sender, PeerEventArgs eventArgs)
        {
            var eventData = eventArgs.EventData;
            var senderPeer = (SocketPeer) sender;
            if (!Peers.Contains(senderPeer))
                return;

            Console.WriteLine($"Peer {Peers.FindIndex(p => p == senderPeer)}: EVENT {eventData.EventType}");

            if (eventData.EventType == PeerEventType.ConnectionClosed)
            {
                lock (peerLock)
                {
                    if (!Peers.Contains(senderPeer))
                        return;

                    senderPeer.Dispose();
                    Peers.Remove(senderPeer);
                }
            }
            //TODO: Implament
        }
    }
}
