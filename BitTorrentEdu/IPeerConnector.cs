﻿using System.Collections.Generic;
using BitTorrentEdu.DTOs;
using Sockets;

namespace BitTorrentEdu
{
    public interface IPeerConnector
    {
        List<SocketPeer> Peers { get; }
        ITcpSocketHelper TcpSocketHelper { get; }

        bool IsPeerConnected(Peer peer);
        bool TryConnectToPeer(Peer peer);
    }
}