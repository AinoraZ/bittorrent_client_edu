using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BitTorrentEdu.DTOs
{
    public class PeerEventArgs : EventArgs
    {
        public PeerEventData EventData { get; }

        public PeerEventArgs(PeerEventData eventData)
        {
            EventData = eventData;
        }
    }

    public class PeerEventData
    {
        public PeerEventStatus EventStatus {get; private set; }
        public PeerEventType EventType { get; private set; }
        public long Length { get; private set; }
        public byte[] Payload { get; private set; }
        public string ErrorMessage { get; private set; }

        public PeerEventData() { }

        public PeerEventData(PeerEventStatus eventStatus, PeerEventType peerEventType, long length, byte[] payload, string errorMsg = null)
        {
            EventStatus = eventStatus;
            EventType = peerEventType;
            Length = length;
            Payload = payload;
            ErrorMessage = errorMsg;
        }
    }

    public enum PeerEventType
    {
        KeepAlive = -1,
        Choke = 0,
        Unchoke = 1,
        Interested = 2,
        NotInterested = 3,
        Have = 4,
        Bitfield = 5,
        Request = 6,
        Piece = 7,
        ConnectionClosed = 256,
        Unknown = 300
    }

    public enum PeerEventStatus
    {
        Ok,
        Partial,
        Error
    }
}
