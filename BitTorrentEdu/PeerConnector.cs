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
        public string PeerId { get;  }

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
            return Peers.Any(p => p.Peer.Ip == peer.Ip);
        }

        public bool TryConnectToPeer (Peer peer)
        {
            if (IsPeerConnected(peer))
                return false;

            if (!TcpSocketHelper.TryEstablishConnection(peer.Ip, peer.Port, out Socket socket))
                return false;

            var socketPeer = new SocketPeer(peer, socket, InfoHash, PeerId);
            Peers.Add(socketPeer);

            return true;
        }
    }
}
