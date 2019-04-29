using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentEdu.DTOs
{
    public class TrackerResponse
    {
        public TrackerResponse(long interval, List<Peer> peers, string trackerId = null, long? minInterval = null,
            long? completeCount = null, long? incompleteCount = null, string warningMessage = null)
        {
            Interval = interval;
            Peers = peers;
            TrackerId = trackerId;
            MinInterval = minInterval;
            CompleteCount = completeCount;
            IncompleteCount = incompleteCount;
            WarningMessage = warningMessage;
        }

        public string WarningMessage { get; private set; }
        public long Interval { get; private set; }
        public long? MinInterval { get; private set; }
        public string TrackerId { get; private set; }
        public long? CompleteCount { get; private set; }
        public long? IncompleteCount { get; private set; }
        public List<Peer> Peers { get; private set; }
    }
}
