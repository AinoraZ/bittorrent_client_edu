using BitTorrentEdu.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentEdu
{
    public class PeerEventDataFactory
    {
        private Dictionary<PeerEventType, int> EventTypesWithKnownLength = new Dictionary<PeerEventType, int>()
        {
            { PeerEventType.Choke, 1},
            { PeerEventType.Unchoke, 1},
            { PeerEventType.Interested, 1},
            { PeerEventType.NotInterested, 1},
            { PeerEventType.Have, 5},
            { PeerEventType.Request, 13},
        };

        public PeerEventDataWrapper TryParsePeerEventDataFromEnumerable(IEnumerable<byte> byteContent)
        {
            return TryParsePeerEventDataFromByteArray(byteContent.ToArray());
        }

        public PeerEventDataWrapper TryParsePeerEventDataFromByteArray(byte[] byteContent)
        {
            if (byteContent.Length == 0)
            {
                var peerEventData = new PeerEventData(PeerEventStatus.Error, PeerEventType.ConnectionClosed, null, "0 bytes sent, closed");
                return new PeerEventDataWrapper(peerEventData, new byte[0]);
            }

            if (byteContent.Length < 4)
            {
                var peerEventData = new PeerEventData(PeerEventStatus.Partial, PeerEventType.Unknown, null);
                return new PeerEventDataWrapper(peerEventData, byteContent); //everything sent is leftover, because it cannot be parsed yet
            }

            var length = ParseLength(byteContent);
            if (length == 0)
            {
                var leftovers = byteContent.Skip(4);
                var peerEventData = new PeerEventData(PeerEventStatus.Ok, PeerEventType.KeepAlive, null);
                return new PeerEventDataWrapper(peerEventData, leftovers.ToArray()); //No leftovers
            }

            if (byteContent.Length == 4)
            {
                var peerEventData = new PeerEventData(PeerEventStatus.Partial, PeerEventType.Unknown, null);
                return new PeerEventDataWrapper(peerEventData, byteContent); //everything sent is leftover, because it cannot be parsed yet
            }

            var eventType = ParseEventType(byteContent); //Single byte for message ID
            var payloadBytes = byteContent.Skip(5).ToArray();

            if (!Enum.IsDefined(typeof(PeerEventType), eventType))
            {
                //Event types are predefined and known in advance. If message id is not in the known event types
                //That could mean this implamentation might not support it, some packets might have been lost or the client is incorrect. 
                //Nothing else to do but close the connection, as returning to a good state will be too hard (or impossible)
                var peerEventData = new PeerEventData(PeerEventStatus.Error, PeerEventType.ConnectionClosed, null, $"Unexpected event type: {(int) eventType}");
                return new PeerEventDataWrapper(peerEventData, new byte[0]); //Excess data is thrown away as the connection will be closed
            }

            if (EventTypesWithKnownLength.TryGetValue(eventType, out int expectedLength) && length != expectedLength)
            {
                //Length for some event types is known in advance. 
                //A different length on a known event type indicates that some packets might have been lost or the client is incorrect. 
                //Nothing else to do but close the connection, as returning to a good state will be too hard (or impossible)
                var errorMessage = $"Unexpected length for known event type: Event type: {eventType}, Length: {length}";
                var peerEventData = new PeerEventData(PeerEventStatus.Error, PeerEventType.ConnectionClosed, null, errorMessage);
                return new PeerEventDataWrapper(peerEventData, new byte[0]);
            }

            if (payloadBytes.Length < length - 1)
            {
                var peerEventData = new PeerEventData(PeerEventStatus.Partial, eventType, null);
                return new PeerEventDataWrapper(peerEventData, byteContent); //everything sent is leftover, because it cannot be parsed yet
            }

            //If we got to this point, payload can be parsed
            return ParsePeerEvent(length, eventType, payloadBytes);
        }

        private PeerEventDataWrapper ParsePeerEvent (long length, PeerEventType eventType, byte[] unparsedPayload)
        {
            var leftovers = new List<byte>();
            var payload = new byte[length - 1];
            int index = 0;
            foreach (var unsortedByte in unparsedPayload)
            {
                if (index == length - 1)
                {
                    leftovers.Add(unsortedByte);
                    continue;
                }

                payload[index] = unsortedByte;
                index++;
            }

            var peerEventData =  new PeerEventData(PeerEventStatus.Ok, eventType, payload);
            return new PeerEventDataWrapper(peerEventData, leftovers.ToArray());
        }

        private long ParseLength(byte[] byteContent)
        {
            var lengthBytes = byteContent.Take(4).ToArray();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            return BitConverter.ToUInt32(lengthBytes, 0);
        }

        private PeerEventType ParseEventType(byte[] byteContent)
        {
            return (PeerEventType) byteContent[4];
        }
    }

    public class PeerEventDataWrapper
    {
        public PeerEventData EventData { get; }
        public byte[] UnusedBytes { get; }

        public PeerEventDataWrapper(PeerEventData eventData, byte[] unusedBytes)
        {
            EventData = eventData;
            UnusedBytes = unusedBytes;
        }
    }
}
