using System;
using System.Collections.Generic;
using System.IO;

namespace BitTorrentEdu.DTOs
{
    public class TorrentPiece
    {
        public bool Complete { get; private set; } = false;
        public bool Pending { get; set; } = false;
        public long PieceIndex { get; }
        public long PieceLength { get; }
        public string SaveDirectory { get; }
        private List<byte> PieceData { get; set; } = new List<byte>();

        //TODO: fix non testable code
        public TorrentPiece(long pieceIndex, long pieceLength, string saveDirectory)
        {
            PieceIndex = pieceIndex;
            PieceLength = pieceLength;
            SaveDirectory = saveDirectory;
        }

        public bool TryAddBlock(byte[] block, uint offset)
        {
            Pending = false;
            if (offset != PieceData.Count)
                return false;

            if (PieceData.Count + block.Length > PieceLength)
                return false;

            PieceData.AddRange(block);
            Complete = PieceData.Count == PieceLength;

            if (Complete)
                StartCompleteSequence();

            return true;
        }

        public void StartCompleteSequence()
        {
            //Console.WriteLine($"Piece {PieceIndex}: Completed");
            if(!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);

            File.WriteAllBytes(GetFullPath(), PieceData.ToArray());
            PieceData = null; //Let the bytes be gc'ed
        }

        public string GetFileName()
        {
            return string.Format("{0, 10:D10}.piece", (uint)PieceIndex);
        }

        public string GetFullPath()
        {
            return Path.Combine(SaveDirectory, GetFileName());
        }

        public Block GetNextBlock()
        {
            var offset = (uint) PieceData.Count;
            var length = (uint) Math.Min(Constants.DefaultPieceSize, PieceLength - PieceData.Count);

            return new Block(offset, length);
        }

        public bool IsCompletedOrPending()
        {
            return Complete || Pending;
        }
    }

    public class Block
    {
        public uint Offset { get; }
        public uint Length { get; }

        public Block(uint offset, uint length)
        {
            Offset = offset;
            Length = length;
        }
    }
}
