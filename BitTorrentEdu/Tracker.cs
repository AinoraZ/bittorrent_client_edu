﻿using Bencode;
using BitTorrentEdu.DTOs;
using Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentEdu
{
    public class Tracker
    {
        private const int PeerIdLength = 20; //According to specification https://wiki.theory.org/index.php/BitTorrentSpecification#Tracker_Request_Parameters
        private const int MinPortNumber = 6881; //According to specification https://wiki.theory.org/index.php/BitTorrentSpecification#Tracker_Request_Parameters
        private const int MaxPortNumber = 6889; //According to specification https://wiki.theory.org/index.php/BitTorrentSpecification#Tracker_Request_Parameters

        private IBencodeParser BencodeParser { get; set; }
        private IHttpClientHelper HttpClient { get; set; }
        private ITrackerResponseFactory TrackerResponseFactory { get; set; }
        public string PeerId { get; private set; }
        public int Port { get; private set; }

        private const string TrackerUriFormat = "?info_hash={0}&peer_id={1}&port={2}&uploaded={3}&downloaded={4}&left={5}&event={6}&compact={7}";

        public Tracker(IHttpClientHelper httpClient, IBencodeParser bencodeParser, ITrackerResponseFactory trackerResponseFactory, string peerId, int port)
        {
            if (peerId?.Length != PeerIdLength)
                throw new ArgumentException($"Peer Id must be {PeerIdLength} characters");

            if (port < MinPortNumber || port > MaxPortNumber)
                throw new ArgumentException($"Port must be in range [{MinPortNumber}, {MaxPortNumber}]");

            BencodeParser = bencodeParser;
            HttpClient = httpClient;
            TrackerResponseFactory = trackerResponseFactory;
            PeerId = peerId;
            Port = port;
        }

        //http://bttracker.debian.org:6969/announce
        public async Task Track(Torrent torrent, TrackerEvent trackerEvent, bool compact = false)
        {
            var hostUrl = torrent.AnnounceUrl;
            var encodedInfoHash = torrent.Info.GetUrlEncodedInfoHash();

            var formattedTrackerRequestData = FormatTrackerRequestData(encodedInfoHash, torrent.Uploaded, torrent.Downloaded, torrent.Left, trackerEvent, compact);

            var fullUrl = hostUrl + formattedTrackerRequestData;
            var responseWrapper = await HttpClient.Send(HttpMethod.Get, fullUrl);
            if (!responseWrapper.IsSuccessStatusCode())
            {
                throw new Exception("Request failed");
            }

            var bytes = responseWrapper.ByteContent;
            var trackerReponse = TrackerResponseFactory.GetTrackerResponse(ref bytes);
        }

        private string FormatTrackerRequestData(string infoHash, long uploaded, long downloaded, long left, TrackerEvent trackerEvent, bool compact)
        {
            var trackerEventString = TrackerEventToString(trackerEvent);
            return string.Format(TrackerUriFormat, infoHash, PeerId, Port, uploaded, downloaded, left, trackerEventString, compact? 1 : 0);
        }

        private string TrackerEventToString(TrackerEvent trackerEvent)
        {
            if (trackerEvent == TrackerEvent.Started) return "started";
            if (trackerEvent == TrackerEvent.Completed) return "completed";
            if (trackerEvent == TrackerEvent.Stopped) return "stopped";

            throw new Exception("Not supported tracker event format");
        }
    }

    public enum TrackerEvent
    {
        Started,
        Stopped,
        Completed
    }
}
