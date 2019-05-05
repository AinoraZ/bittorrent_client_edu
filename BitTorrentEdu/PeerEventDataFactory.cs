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
        private Dictionary<PeerEventType, int> EventTypesWithKnownLength= new Dictionary<PeerEventType, int>()
        {
            { PeerEventType.Choke, 1},
            { PeerEventType.Unchoke, 1},
            { PeerEventType.Interested, 1},
            { PeerEventType.NotInterested, 1},
            { PeerEventType.Have, 5},
            { PeerEventType.Request, 13},
        };

        public PeerEventData GeneratePeerEventDataFromByteArray (byte[] byteContent)
        {
            PeerEventType eventType;

            if (byteContent.Length == 0)
                return new PeerEventData(PeerEventStatus.Ok, PeerEventType.ConnectionClosed, 0, null);

            if (byteContent.Length < 4)
                return new PeerEventData(PeerEventStatus.Error, PeerEventType.ConnectionClosed, byteContent.Length, null);

            var lengthBytes = byteContent.Take(4).ToArray();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            var length = BitConverter.ToUInt32(lengthBytes, 0);
            if (length == 0)
                return new PeerEventData(PeerEventStatus.Ok, PeerEventType.KeepAlive, 4, null);

            var offset = 4;
            eventType = (PeerEventType) byteContent.Skip(offset).Take(1).Single(); //Single byte for message ID

            offset++;
            var payloadBytes = byteContent.Skip(offset).ToArray();

            if (EventTypesWithKnownLength.TryGetValue(eventType, out int expectedLength))
                if(expectedLength != length)
                    return new PeerEventData(PeerEventStatus.Error, PeerEventType.ConnectionClosed, length, payloadBytes);

            if (!Enum.IsDefined(typeof(PeerEventType), eventType))
                return new PeerEventData(PeerEventStatus.Error, PeerEventType.ConnectionClosed, length, payloadBytes);

            if (payloadBytes.Length > length - 1)
                return new PeerEventData(PeerEventStatus.Error, PeerEventType.ConnectionClosed, length, payloadBytes);

            var eventStatus = (payloadBytes.Length == length - 1) ? PeerEventStatus.Ok : PeerEventStatus.Partial;

            var forLength = Math.Min(payloadBytes.Length, length - 1);
            return new PeerEventData(eventStatus, eventType, length, payloadBytes);
        }
    }
}
