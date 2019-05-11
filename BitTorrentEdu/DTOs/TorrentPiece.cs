using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BitTorrentEdu.DTOs
{
    public class TorrentPiece
    {
        public bool Complete { get; private set; } = false;
        public long PieceIndex { get; }
        public long PieceLength { get; }
        public string SaveDirectory { get; }
        private List<byte> PieceData { get; set; } = new List<byte>();
        private byte[] ExpectedPieceHash { get; }
        
        //TODO: fix non testable code
        public TorrentPiece(long pieceIndex, long pieceLength, string saveDirectory, string pieceHash)
        {
            PieceIndex = pieceIndex;
            PieceLength = pieceLength;
            SaveDirectory = saveDirectory;
            ExpectedPieceHash = Encoding.GetEncoding(28591).GetBytes(pieceHash);

            //TODO: Need file operation wrapper, bad practice
            if (File.Exists(GetFullPath()))
                Complete = true;
        }

        public bool TryAddBlock(byte[] block, uint offset)
        {
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

        //TODO: Make wrapper of file operations for testability
        public void StartCompleteSequence()
        {
            if (!HashPiece().SequenceEqual(ExpectedPieceHash))
            {
                Complete = false;
                PieceData = new List<byte>();
                return;
            }

            if(!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);

            File.WriteAllBytes(GetFullPath(), PieceData.ToArray());
            PieceData = null; //Let the bytes be gc'ed
        }

        private byte[] HashPiece()
        {
            return new SHA1Managed().ComputeHash(PieceData.ToArray());
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
