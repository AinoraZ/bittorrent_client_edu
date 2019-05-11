using System;
using System.Collections.Generic;
using BitTorrentEdu.DTOs;

namespace BitTorrentEdu
{
    public interface IPeerConnector
    {
        List<SocketPeer> Peers { get; }

        void DisconnectPeer(SocketPeer peer);
        bool IsPeerConnected(Peer peer);
        bool TryConnectToPeer(Peer peer, EventHandler<PeerEventArgs> eventHandler = null);
    }
}