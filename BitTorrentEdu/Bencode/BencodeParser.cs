using Bencode.BencodeParsers;
using Bencode.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Bencode
{
    public class BencodeParser : IBencodeParser
    {
        public IBencodeParserFactory BencodeParserFactory { get; }

        public BencodeParser(IBencodeParserFactory bencodeParserFactory = null)
        {
            BencodeParserFactory = bencodeParserFactory ?? new BencodeParserFactory();
        }

        public List<BencodedObject> ParseAllBencodeFromFile(string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            return ParseAllBencodeFromBytes(ref bytes);
        }

        public List<BencodedObject> ParseAllBencodeFromString(string bencodedString)
        {
            var bytes = Encoding.GetEncoding(1252).GetBytes(bencodedString);
            return ParseAllBencodeFromBytes(ref bytes);
        }

        public List<BencodedObject> ParseAllBencodeFromBytes(ref byte[] bytes)
        {
            var returnValue = new List<BencodedObject>();
            int byteOffset = 0;

            while (byteOffset < bytes.Length)
            {
                var initial = BencodeParserBase.GetInitialByte(ref bytes, ref byteOffset);

                var parser = BencodeParserFactory.CreateParser(initial);
                var bencodeObj = parser.Parse(ref bytes, ref byteOffset);

                returnValue.Add(bencodeObj);
            }

            return returnValue;
        }
    }
}
