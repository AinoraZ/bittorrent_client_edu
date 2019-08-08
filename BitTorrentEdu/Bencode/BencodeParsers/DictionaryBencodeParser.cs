using System.Collections.Generic;
using Bencode.DTOs;

namespace Bencode.BencodeParsers
{
    public class DictionaryBencodeParser : BencodeParserBase
    {
        private IBencodeParserFactory ParserFactory { get; set; }

        public DictionaryBencodeParser(IBencodeParserFactory parserFactory)
        {
            ParserFactory = parserFactory;
        }

        public override BencodedObject Parse(ref byte[] bytes, ref int byteOffset)
        {
            string rawValue = BencodeConstants.DictionaryInitial.ToString();
            var tempValue = new Dictionary<string, BencodedObject>();
            while (ByteToChar(bytes[byteOffset]) != BencodeConstants.EndingCharacter)
            {
                byteOffset++; //Fake initial read, as we know it should be a string
                var stringParser = ParserFactory.CreateParser(BencodeConstants.StringInitial);
                var bencodedString = (BencodedString)stringParser.Parse(ref bytes, ref byteOffset);
                rawValue += bencodedString.RawValue;

                var initial = GetInitialByte(ref bytes, ref byteOffset);
                var parser = ParserFactory.CreateParser(initial);

                var bencodedObj = parser.Parse(ref bytes, ref byteOffset);
                rawValue += bencodedObj.RawValue;

                tempValue.Add(bencodedString.Value, bencodedObj);
            }
            byteOffset++;
            rawValue += BencodeConstants.EndingCharacter;

            return new BencodedDictionary(tempValue, rawValue);
        }
    }
}
