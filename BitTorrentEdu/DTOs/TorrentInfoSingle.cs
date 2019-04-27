using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentEdu.DTOs
{
    public class TorrentInfoSingle
    {
        private const int pieceHashLength = 40;

        public TorrentInfoSingle(string infoHash, int pieceLength, int length, string name = null, string md5Sum = null)
        {
            InfoHash = infoHash;
            PieceLength = pieceLength;
            Length = length;
            Name = name;
            Md5Sum = md5Sum;
        }

        public TorrentInfoSingle(string infoHash, int pieceLength, int length, List<string> pieceHashes, string name = null, string md5Sum = null) 
            : this(infoHash, pieceLength, length, name, md5Sum)
        {
            pieceHashes.ForEach(AddPieceHash);
        }

        public string InfoHash { get; private set; }
        public int PieceLength { get; private set; }
        public string Name { get; private set; }
        public int Length { get; private set; }
        public string Md5Sum { get; private set; }
        public List<string> PieceHashes { get; private set; } = new List<string>();

        public void AddPieceHash(string pieceHash)
        {
            if (pieceHash.Length != 40)
                throw new ArgumentException($"Piece hash must be {pieceHashLength} characters long");

            PieceHashes.Add(pieceHash);
        }
    }
}
