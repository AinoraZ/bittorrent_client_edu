using Bencode;
using Bencode.DTOs;
using BitTorrentEdu.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentEdu
{
    public class TrackerResponseFactory : ITrackerResponseFactory
    {
        private IBencodeParser BencodeParser { get; }

        public TrackerResponseFactory(IBencodeParser bencodeParser)
        {
            BencodeParser = bencodeParser;
        }

        public TrackerResponse GetTrackerResponse(ref byte[] bytes)
        {
            var bencodedObjs = BencodeParser.ParseAllBencodeFromBytes(ref bytes);
            if (bencodedObjs.Count != 1)
                throw new Exception("Tracker response in bad format. Response must be a single bencoded dictionary");

            var bencodedObj = bencodedObjs.FirstOrDefault();
            if (bencodedObj.Type != BencodedType.Dictionary)
                throw new Exception("Tracker response in bad format. Response must be a single bencoded dictionary");

            var bencodedDict = ((BencodedDictionary) bencodedObj).Value;

            ProcessFailureReasonIfAny(bencodedDict);
            var interval = GetInterval(bencodedDict);
            var minInterval = GetMinIntervalIfAny(bencodedDict);
            var trackerId = GetTrackerIdIfAny(bencodedDict);
            var peers = GetPeers(bencodedDict);

            return new TrackerResponse(interval, peers, trackerId, minInterval);
        }

        private void ProcessFailureReasonIfAny(Dictionary<string, BencodedObject> bencodedTrackerResponse)
        {
            if (!bencodedTrackerResponse.TryGetValue("failure reason", out BencodedObject failureObj))
                return; //No failure, continue

            if (failureObj.Type != BencodedType.String)
                throw new Exception("Tracker response in bad format. Tracker responded with non string failure message");

            var failureString = ((BencodedString) failureObj).Value;
            throw new Exception($"Tracker responded with failure. Exception: {failureString}");
        }

        private long GetInterval(Dictionary<string, BencodedObject> bencodedTrackerResponse)
        {
            if (!bencodedTrackerResponse.TryGetValue("interval", out BencodedObject bencodedIntervalObj))
                throw new Exception("Tracker response in bad format. Tracker did not contain an interval value");
            if (bencodedIntervalObj.Type != BencodedType.Integer)
                throw new Exception("Tracker response in bad format. Tracker interval was a non integer value");

            return ((BencodedInteger) bencodedIntervalObj).Value;
        }

        private long? GetMinIntervalIfAny(Dictionary<string, BencodedObject> bencodedTrackerResponse)
        {
            if (!bencodedTrackerResponse.TryGetValue("min interval", out BencodedObject bencodedMinIntervalObj))
                return null;

            if (bencodedMinIntervalObj.Type != BencodedType.Integer)
                throw new Exception("Tracker response in bad format. Tracker min interval was a non integer value");

            return ((BencodedInteger) bencodedMinIntervalObj).Value;
        }

        private string GetTrackerIdIfAny(Dictionary<string, BencodedObject> bencodedTrackerResponse)
        {
            if (!bencodedTrackerResponse.TryGetValue("tracker id", out BencodedObject bencodedTrackerId))
                return null;

            if (bencodedTrackerId.Type != BencodedType.String)
                throw new Exception("Tracker response in bad format. Tracker id was a non string value");

            return ((BencodedString)bencodedTrackerId).Value;
        }

        private List<Peer> GetPeers(Dictionary<string, BencodedObject> bencodedTrackerResponse)
        {
            if (!bencodedTrackerResponse.TryGetValue("peers", out BencodedObject bencodedPeers))
                throw new Exception("Trackers without ipv4 peer lists are not suppported");

            if (bencodedPeers.Type == BencodedType.Dictionary)
            {
                var dictionaryPeers = ((BencodedDictionary)bencodedPeers).Value;
                return GetDictionaryPeers(dictionaryPeers);
            }

            if (bencodedPeers.Type == BencodedType.String)
            {
                var byteStringPeers = ((BencodedString) bencodedPeers).Value;
                return GetByteStringPeers(byteStringPeers);
            }

            throw new Exception("Tracker response in bad format. Peers must be in a dictionary or binary string model");
        }

        private List<Peer> GetDictionaryPeers(Dictionary<string, BencodedObject> bencodedPeers)
        {
            throw new NotImplementedException();
        }

        private List<Peer> GetByteStringPeers(string byteStringPeers)
        {
            var peers = new List<Peer>();

            var bytePeers = Encoding.GetEncoding(28591).GetBytes(byteStringPeers);
            if (bytePeers.Length % 6 != 0)
                throw new Exception("ByteString must be of format: 4 bytes ip, 2 bytes port");

            for (int index = 0; index < bytePeers.Length / 6; index++)
            {
                var peerBytes = bytePeers.Skip(6 * index).Take(6);
                var addressBytes = peerBytes.Take(4).ToArray();
                var ip = new IPAddress(addressBytes);

                var portBytes = peerBytes.Skip(4).Take(2).ToArray();
                if (BitConverter.IsLittleEndian)
                    portBytes = portBytes.Reverse().ToArray();

                var port = BitConverter.ToUInt16(portBytes, 0);

                var peer = new Peer(ip, port);
                peers.Add(peer);
            }

            return peers;
        }
    }
}
