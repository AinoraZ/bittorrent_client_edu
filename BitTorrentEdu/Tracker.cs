using BitTorrentEdu.DTOs;
using Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentEdu
{
    public class Tracker
    {
        private const int PeerIdLength = 20; //According to specification https://wiki.theory.org/index.php/BitTorrentSpecification#Tracker_Request_Parameters
        private const int MinPortNumber = 6881; //According to specification https://wiki.theory.org/index.php/BitTorrentSpecification#Tracker_Request_Parameters
        private const int MaxPortNumber = 6889; //According to specification https://wiki.theory.org/index.php/BitTorrentSpecification#Tracker_Request_Parameters

        private IHttpClientHelper HttpClient { get; set; }
        public string PeerId { get; private set; }
        public int Port { get; private set; }

        private const string TrackerUriFormat = "?info_hash={0}&peer_id={1}&port={2}&uploaded={3}&downloaded={4}&left={5}&event={6}";

        public Tracker(IHttpClientHelper httpClient, string peerId, int port)
        {
            if (peerId?.Length != PeerIdLength)
                throw new ArgumentException($"Peer Id must be {PeerIdLength} characters");

            if (port < MinPortNumber || port > MaxPortNumber)
                throw new ArgumentException($"Port must be in range [{MinPortNumber}, {MaxPortNumber}]");

            HttpClient = httpClient;
            PeerId = peerId;
            Port = port;
        }

        //http://bttracker.debian.org:6969/announce
        public void Track(Torrent torrent, TrackerEvent trackerEvent, bool compact = false)
        {
            var hostUrl = torrent.AnounceUrl;
        }

        public string FormatTrackerRequestData(string infoHash, int uploaded, int downloaded, int left, TrackerEvent trackerEvent)
        {
            throw new NotImplementedException();
        }
    }

    public enum TrackerEvent
    {
        Started,
        Stopped,
        Comppleted
    }
}
