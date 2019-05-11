using System.Threading.Tasks;
using BitTorrentEdu.DTOs;

namespace BitTorrentEdu
{
    public interface ITracker
    {
        string PeerId { get; }
        int Port { get; }

        Task<TrackerResponse> Track(Torrent torrent, TrackerEvent trackerEvent, bool compact = false);
    }
}