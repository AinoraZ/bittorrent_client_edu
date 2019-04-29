using System;
using System.Collections.Generic;
using System.Linq;

namespace BitTorrentEdu.DTOs
{
    public class TorrentInfoSingle
    {
        public TorrentInfoSingle(byte[] infoHash, long pieceLength, long length, string name = null, string md5Sum = null)
        {
            InfoHash = infoHash;
            PieceLength = pieceLength;
            Length = length;
            Name = name;
            Md5Sum = md5Sum;
        }

        public TorrentInfoSingle(byte[] infoHash, long pieceLength, long length, List<string> pieceHashes, string name = null, string md5Sum = null) 
            : this(infoHash, pieceLength, length, name, md5Sum)
        {
            pieceHashes.ForEach(AddPieceHash);
        }

        public byte[] InfoHash { get; private set; }
        public long PieceLength { get; private set; }
        public string Name { get; private set; }
        public long Length { get; private set; }
        public string Md5Sum { get; private set; }
        public List<string> PieceHashes { get; private set; } = new List<string>();

        public string GetUrlEncodedInfoHash()
        {
            var bytesFormatted = InfoHash.Select(b => string.Format("{0:X2}", b));
            return "%" + string.Join("%", bytesFormatted);
        }

        public void AddPieceHash(string pieceHash)
        {
            if (pieceHash.Length != Constants.PieceHashLength)
                throw new ArgumentException($"Piece hash must be {Constants.PieceHashLength} characters long");

            PieceHashes.Add(pieceHash);
        }
    }
}
