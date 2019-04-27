using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentEdu.DTOs
{
    public class TrackerResponse
    {
        public string FailureReason { get; set; }
        public string WarningMessage { get; set; }
        public int Interval { get; set; }
        public int? MinInterval { get; set; }
        public string TrackerId { get; set; }
        public int CompleteCount { get; set; }
        public int IncompleteCount { get; set; }
        public List<Peer> Peers { get; set; }
    }
}
