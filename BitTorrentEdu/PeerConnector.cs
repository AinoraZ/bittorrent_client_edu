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
        private readonly object peerLock = new object();

        private List<SocketPeer> _peers = new List<SocketPeer>();
        public List<SocketPeer> Peers {
            get 
            {
                lock (peerLock)
                {
                    return _peers.ToList();
                }
            }
        }

        public ITcpSocketHelper TcpSocketHelper { get; private set; }
        public byte[] InfoHash { get; }
        public string PeerId { get; }

        private List<Peer> PendingPeers { get; set; } = new List<Peer>();

        public PeerConnector(ITcpSocketHelper tcpSocketHelper, string peerId, TorrentInfoSingle torrentInfo)
        {
            if (torrentInfo.InfoHash.Length != Constants.InfoHashLength)
                throw new ArgumentException($"Info hash must be {Constants.InfoHashLength} bytes");

            if (peerId.Length != Constants.PeerIdLength)
                throw new ArgumentException($"Peer id must be {Constants.PeerIdLength} bytes");

            TcpSocketHelper = tcpSocketHelper;
            InfoHash = torrentInfo.InfoHash;
            PeerId = peerId;
        }

        public bool IsPeerConnected (Peer peer)
        {
            return _peers.Any(p => p.Peer.Ip.Equals(peer.Ip)) || PendingPeers.Any(p => p.Ip.Equals(peer.Ip));
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
                _peers.Add(socketPeer);
                PendingPeers.Remove(peer);
            }

            socketPeer.StartReceive();

            return true;
        }

        public void OnPeerEvent(object sender, PeerEventArgs eventArgs)
        {
            var eventData = eventArgs.EventData;
            var senderPeer = (SocketPeer) sender;
            lock (peerLock)
            {
                if (!Peers.Contains(senderPeer))
                    return;
            }

            if (eventData.EventType == PeerEventType.ConnectionClosed)
            {
                Console.WriteLine($"Peer {senderPeer.Peer.Ip}: EVENT {eventData.EventType}: Error: {eventData.ErrorMessage}");
                lock (peerLock)
                {
                    if (!_peers.Contains(senderPeer))
                        return;

                    senderPeer.Dispose();
                    _peers.Remove(senderPeer);
                    return;
                }
            }
        }
    }
}
