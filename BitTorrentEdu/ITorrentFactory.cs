using BitTorrentEdu.DTOs;

namespace BitTorrentEdu
{
    public interface ITorrentFactory
    {
        Torrent GetTorrentFromFile(string filePath);
    }
}