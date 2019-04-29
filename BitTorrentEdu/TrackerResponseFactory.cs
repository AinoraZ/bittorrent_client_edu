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

            if (bencodedPeers.Type == BencodedType.List)
            {
                var dictionaryPeers = ((BencodedList) bencodedPeers).Value;
                return GetDictionaryPeers(dictionaryPeers);
            }

            if (bencodedPeers.Type == BencodedType.String)
            {
                var byteStringPeers = ((BencodedString) bencodedPeers).Value;
                return GetByteStringPeers(byteStringPeers);
            }

            throw new Exception("Tracker response in bad format. Peers must be in a dictionary or binary string model");
        }

        private List<Peer> GetDictionaryPeers(List<BencodedObject> bencodedPeers)
        {
            var resultPeers = new List<Peer>();

            foreach (var bencodedPeer in bencodedPeers)
            {
                if (bencodedPeer.Type != BencodedType.Dictionary)
                    throw new Exception("Each peer list item must be a dictionary");

                var bencodedPeerDict = ((BencodedDictionary) bencodedPeer).Value;
                var ipStr = GetIpAddress(bencodedPeerDict);
                var port = GetPort(bencodedPeerDict);
                var peerId = GetPeerIdIfAny(bencodedPeerDict);

                if (TryParseIPv4String(ipStr, out byte[] byteAddress))
                    throw new Exception("Only IPv4 address are supported");


                var ipAddress = new IPAddress(byteAddress);
                var peer = new Peer(ipAddress, port, peerId);

                resultPeers.Add(peer);
            }

            return resultPeers;
        }

        private bool TryParseIPv4String(string IPv4Address, out byte[] byteAddress)
        {
            byteAddress = new byte[4];
            var decimalStrings = IPv4Address.Split('.');
            if (decimalStrings.Length != 4)
                return false;

            if (decimalStrings.Select(d => d.Length <= 3).Any(b => b == false)) //check if each string is 3 chars or bellow
                return false;

            try
            {
                var decimalInts = decimalStrings.Select(d => Convert.ToInt32(d)).ToList();
                if (decimalInts.Select(i => i <= 255).Any(b => b == false))
                    return false;

                byteAddress = decimalInts.Select(i => (byte) i).ToArray();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetPeerIdIfAny(Dictionary<string, BencodedObject> bencodedPeerDict)
        {
            if (!bencodedPeerDict.TryGetValue("peer id", out BencodedObject bencodedPeerId))
                return null;

            if (bencodedPeerId.Type != BencodedType.String)
                throw new Exception("Peer id must be a string");

            return ((BencodedString) bencodedPeerId).Value;
        }

        private long GetPort(Dictionary<string, BencodedObject> bencodedPeerDict)
        {
            if (!bencodedPeerDict.TryGetValue("port", out BencodedObject bencodedPort))
                throw new Exception("Each peer must contain a Port");

            if (bencodedPort.Type != BencodedType.Integer)
                throw new Exception("Port must be an integer");

            return ((BencodedInteger) bencodedPort).Value;
        }

        private string GetIpAddress(Dictionary<string, BencodedObject> bencodedPeerDict)
        {
            if (!bencodedPeerDict.TryGetValue("ip", out BencodedObject bencodedIp))
                throw new Exception("Each peer must contain an IP address");

            if (bencodedIp.Type != BencodedType.String)
                throw new Exception("Peer ip must be a string");

            return ((BencodedString) bencodedIp).Value;
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
