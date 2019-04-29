using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace BitTorrentEdu.DTOs
{
    public class SocketPeer
    {
        //TODO: Implament an event to listen to when peer disconnects (will be consumed by PeerConnector)

        public Peer Peer { get; }
        public Socket Socket { get; }
        public byte[] InfoHash { get; }
        public string PeerId { get;  }
        public bool AmInterested { get; set; } = false;
        public bool PeerChocking { get; set; } = true;

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

        public async Task InitiateHandsakeAsync()
        {
            await Task.Delay(1);
            throw new NotImplementedException();
        }

        public async Task SendKeepAliveAsync()
        {
            await Task.Delay(1);
            throw new NotImplementedException();
        }

        public async Task SendInterestAsync(bool isInterested)
        {
            await Task.Delay(1);
            throw new NotImplementedException();
        }

        public async Task RequestPieceAsync(int pieceIndex, int blockBeginIndex, int blockLength)
        {
            await Task.Delay(1);
            throw new NotImplementedException();
        }

        //TODO: Find a way to parse response and return results for any type of response supported
        public async Task RecieveResponse()
        {
            await Task.Delay(1);
            throw new NotImplementedException();
        }
    }
}
