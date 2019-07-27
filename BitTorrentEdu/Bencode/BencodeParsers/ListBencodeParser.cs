using System.Collections.Generic;
using Bencode.DTOs;

namespace Bencode.BencodeParsers
{
    internal class ListBencodeParser : BencodeParserBase
    {
        private IBencodeParserFactory ParserFactory { get; set; }

        public ListBencodeParser(IBencodeParserFactory parserFactory)
        {
            ParserFactory = parserFactory;
        }

        internal override BencodedObject Parse(ref byte[] bytes, ref int byteOffset)
        {
            string rawValue = BencodeConstants.ListInitial.ToString();
            var tempValue = new List<BencodedObject>();
            while (ByteToChar(bytes[byteOffset]) != BencodeConstants.EndingCharacter)
            {
                var initial = GetInitialByte(ref bytes, ref byteOffset);
                var parser = ParserFactory.CreateParser(initial);

                var bencodedObj = parser.Parse(ref bytes, ref byteOffset);
                rawValue += bencodedObj.RawValue;

                tempValue.Add(bencodedObj);
            }
            byteOffset++;
            rawValue += BencodeConstants.EndingCharacter;

            return new BencodedList(tempValue, rawValue);
        }
    }
}
