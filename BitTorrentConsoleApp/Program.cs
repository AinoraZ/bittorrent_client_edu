using Bencode;
using BitTorrentEdu;
using BitTorrentEdu.DTOs;
using Sockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BitTorrentConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine($"You must pass 3 arguments: Path to torrent, Download directory, Port (Range: [{Constants.MinPortNumber}:{Constants.MaxPortNumber}])");
                return;
            }

            var torrentPath = args[0];
            if (!File.Exists(torrentPath))
            {
                Console.Error.WriteLine("Passed torrent does not exist");
                return;
            }

            var saveDir = args[1];
            if (!int.TryParse(args[2], out int port))
            {
                Console.Error.WriteLine("Invalid port int passed");
                return;
            }

            if (port > Constants.MaxPortNumber || port < Constants.MinPortNumber)
            {
                Console.Error.WriteLine($"Port must be in range: [{Constants.MinPortNumber}:{Constants.MaxPortNumber}]");
                return;
            }

            var peerId = "-ZA0001-000000000001";
            var bencodeParser = new BencodeParser();
            var httpClient = new HttpClientHelper();
            var tcpSocketHelper = new TcpSocketHelper();
            var torrentFactory = new TorrentFactory(bencodeParser);
            var trackerResponseFactory = new TrackerResponseFactory(bencodeParser);
            var peerEventDataFactory = new PeerEventDataFactory();

            var torrentClient = new BitTorrentDownloader(peerId, bencodeParser, httpClient, tcpSocketHelper, 
                torrentFactory, trackerResponseFactory, peerEventDataFactory);

            torrentClient.PeerEventHandlerEcho += OnPeerEvent;
            var torrentThread = new Thread(() => torrentClient.DownloadTorrent(torrentPath, saveDir, port));
            torrentThread.Start();

            Console.Write("Downloading file... ");
            using (var progress = new ProgressBar(40))
            {
                while (!torrentClient.IsDownloadCompleted)
                {
                    Thread.Sleep(Constants.UpdateClockMs);
                    var pieces = torrentClient.Pieces;
                    if (pieces.Count == 0)
                        continue;

                    var completedPieces = pieces.Count - torrentClient.GetNonCompletedPieces().Count;
                    progress.Report((double) completedPieces / pieces.Count);
                }

                progress.Report(1);
            }

            Console.WriteLine("Done downloading file");
            Console.WriteLine("Press any key to continue...");
            Console.ReadLine();
        }

        private static void OnPeerEvent(object sender, PeerEventArgs peerEventArgs)
        {

        }
    }
}
