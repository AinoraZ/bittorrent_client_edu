using Bencode;
using Sockets;

namespace BitTorrentEdu
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new BencodeParser();

            var torrentFactory = new TorrentFactory(parser);
            var torrent = torrentFactory.GetTorrentFromFile(@"G:\University\uzd2\03fd3cba845a8d252d9768806486f004d7f4e374.torrent");

            var httpClient = new HttpClientHelper();
            var trackerResponseFactory = new TrackerResponseFactory(parser);
            var tracker = new Tracker(httpClient, parser, trackerResponseFactory, "-ZA0001-000000000001", 6881);

            tracker.Track(torrent, TrackerEvent.Started).Wait();
        }
    }
}
