﻿using Bencode;
using Bencode.DTOs;
using BitTorrentEdu.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BitTorrentEdu
{
    public class TorrentFactory : ITorrentFactory
    {
        private IBencodeParser BencodeParser { get; set; }

        public TorrentFactory(IBencodeParser bencodeParser)
        {
            BencodeParser = bencodeParser;
        }

        public Torrent GetTorrentFromFile(string filePath)
        {
            var bencodes = BencodeParser.ParseAllBencodeFromFile(filePath);
            if (bencodes.Count != 1)
                throw new Exception("Wrong torrent file format. Should be a single dictionary.");

            var bencodeTorrent = bencodes.FirstOrDefault();
            if (bencodeTorrent.Type != BencodedType.Dictionary)
                throw new Exception("Wrong torrent file format. Bencode should be a dictionary.");

            var bencodedDictionary = ((BencodedDictionary) bencodeTorrent).Value;
            if (!bencodedDictionary.TryGetValue("info", out BencodedObject bencodeInfoObj))
                throw new Exception("Wrong torrent file format. Torrent file does not contain info section.");

            var infoSingle = GetTorrentInfo(bencodeInfoObj);

            if (!bencodedDictionary.TryGetValue("announce", out BencodedObject bencodeAnnounceObj))
                throw new Exception("Wrong torrent file format. Torrent file does not contain announce section.");

            var announceUrl = GetAnnounceUrl(bencodeAnnounceObj);

            return new Torrent(announceUrl, infoSingle);
        }

        private string GetAnnounceUrl(BencodedObject bencodedAnnounceObj)
        {
            if (bencodedAnnounceObj.Type != BencodedType.String)
                throw new Exception("Wrong torrent file format. Torrent announce url must be a string.");

            return ((BencodedString) bencodedAnnounceObj).Value;
        }

        private TorrentInfoSingle GetTorrentInfo(BencodedObject bencodedTorrentInfo)
        {
            if (bencodedTorrentInfo.Type != BencodedType.Dictionary)
                throw new Exception("Wrong torrent file format. Torrent info must be a dictionary.");

            var bencodedInfoDict = (BencodedDictionary) bencodedTorrentInfo;
            var infoDictionary = bencodedInfoDict.Value;

            if (!infoDictionary.TryGetValue("name", out BencodedObject bencodedName))
                throw new Exception("Wrong torrent file format. Info does not contain name information.");

            var name = GetInfoName(bencodedName);

            if (!infoDictionary.TryGetValue("piece length", out BencodedObject bencodePieceLength))
                throw new Exception("Wrong torrent file format. Info does not contain piece length information.");

            var pieceLength = GetInfoPieceLength(bencodePieceLength);

            if (!infoDictionary.TryGetValue("length", out BencodedObject bencodeLength))
                throw new Exception("Wrong torrent file format. Info does not contain length information. This program currently supports sinlge file mode only.");

            var length = GetInfoLength(bencodeLength);

            if (!infoDictionary.TryGetValue("pieces", out BencodedObject bencodePieces))
                throw new Exception("Wrong torrent file format. Info does not contain length information.");

            var pieces = GetInfoPieces(bencodePieces);
            var hash = HashBencodeDictionary(bencodedInfoDict);

            return new TorrentInfoSingle(hash, pieceLength, length, pieces, name);
        }

        private string GetInfoName(BencodedObject bencodedName)
        {
            if (bencodedName.Type != BencodedType.String)
                throw new Exception("Wrong torrent file format. Name should be a string.");

            return ((BencodedString) bencodedName).Value;
        }

        private long GetInfoPieceLength(BencodedObject bencodedPieceLength)
        {
            if (bencodedPieceLength.Type != BencodedType.Integer)
                throw new Exception("Wrong torrent file format. Piece length should be an integer.");

            return ((BencodedInteger) bencodedPieceLength).Value;
        }

        private long GetInfoLength(BencodedObject bencodedLength)
        {
            if (bencodedLength.Type != BencodedType.Integer)
                throw new Exception("Wrong torrent file format. Length should be an integer.");

            return ((BencodedInteger) bencodedLength).Value;
        }

        private List<string> GetInfoPieces(BencodedObject bencodedLength)
        {
            var result = new List<string>();

            if (bencodedLength.Type != BencodedType.String)
                throw new Exception("Wrong torrent file format. Pieces should be a string.");

            var piecesStr = ((BencodedString) bencodedLength).Value;
            if (piecesStr.Length % Constants.PieceHashLength != 0)
                throw new Exception($"Wrong torrent file format. Each piece hash should be {Constants.PieceHashLength} bytes long.");

            for (var i = 0; i < piecesStr.Length; i += Constants.PieceHashLength)
            {
                var pieceHash = piecesStr.Substring(i, Math.Min(Constants.PieceHashLength, piecesStr.Length - i));
                result.Add(pieceHash);
            }

            return result;
        }

        private byte[] HashBencodeDictionary(BencodedDictionary bencodedDictionary)
        {
            return new SHA1Managed().ComputeHash(Encoding.GetEncoding(28591).GetBytes(bencodedDictionary.RawValue));
        }
    }
}
