using BitTorrentEdu.DTOs;

namespace BitTorrentEdu
{
    public interface ITrackerResponseFactory
    {
        TrackerResponse GetTrackerResponse(ref byte[] bytes);
    }
}