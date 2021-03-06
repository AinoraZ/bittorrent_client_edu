﻿using BitTorrentEdu.DTOs;
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

        private string PeerId { get; }
        private TorrentInfoSingle TorrentInfo { get; set; }
        private ITcpSocketHelper TcpSocketHelper { get; }
        private List<Peer> PendingPeers { get; set; } = new List<Peer>();
        private IPeerEventDataFactory PeerEventDataFactory { get; }

        public PeerConnector(IPeerEventDataFactory peerEventDataFactory, ITcpSocketHelper tcpSocketHelper, string peerId, TorrentInfoSingle torrentInfo)
        {
            if (torrentInfo.InfoHash.Length != Constants.InfoHashLength)
                throw new ArgumentException($"Info hash must be {Constants.InfoHashLength} bytes");

            if (peerId.Length != Constants.PeerIdLength)
                throw new ArgumentException($"Peer id must be {Constants.PeerIdLength} bytes");

            PeerEventDataFactory = peerEventDataFactory;
            TcpSocketHelper = tcpSocketHelper;
            TorrentInfo = torrentInfo;
            PeerId = peerId;
        }

        public bool IsPeerConnected (Peer peer)
        {
            return _peers.Any(p => p.Peer.Ip.Equals(peer.Ip)) || PendingPeers.Any(p => p.Ip.Equals(peer.Ip));
        }

        public bool TryConnectToPeer (Peer peer, EventHandler<PeerEventArgs> eventHandler = null)
        {
            lock (peerLock)
            {
                if (IsPeerConnected(peer))
                    return false;

                if (_peers.Count + PendingPeers.Count >= Constants.MaxPeers)
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

            var socketPeer = new SocketPeer(PeerEventDataFactory, peer, socket, TorrentInfo, PeerId);
            if (!socketPeer.TryInitiateHandsake())
            {
                PendingPeers.Remove(peer);
                DisconnectPeer(socketPeer);
                return false;
            }

            socketPeer.PeerEventHandler += OnPeerEvent;
            socketPeer.PeerEventHandler += eventHandler;
            lock (peerLock)
            {
                _peers.Add(socketPeer);
                PendingPeers.Remove(peer);
            }

            socketPeer.StartReceive();

            return true;
        }

        public void DisconnectPeer(SocketPeer peer)
        {
            lock (peerLock)
            {
                if (_peers.Contains(peer))
                    _peers.Remove(peer);

                peer.Dispose();
            }
        }

        private void OnPeerEvent(object sender, PeerEventArgs eventArgs)
        {
            var eventData = eventArgs.EventData;
            var senderPeer = (SocketPeer) sender;
            if (!Peers.Contains(senderPeer))
                return;

            if (eventData.EventType == PeerEventType.ConnectionClosed)
                DisconnectPeer(senderPeer);
        }
    }
}
