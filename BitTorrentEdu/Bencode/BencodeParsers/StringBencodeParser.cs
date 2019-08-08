using System.Collections.Generic;
using Bencode.DTOs;

namespace Bencode.BencodeParsers
{
    public class StringBencodeParser : BencodeParserBase
    {
        public override BencodedObject Parse(ref byte[] bytes, ref int byteOffset)
        {
            byteOffset--;
            var strLength = ParseIntegerUntilCharacter(ref bytes, ref byteOffset, BencodeConstants.StringDelimter);
            string rawValue = $"{strLength}{BencodeConstants.StringDelimter}";
            var offsetLength = byteOffset + strLength;

            var tempChars = new List<char>();
            for (; byteOffset < offsetLength; byteOffset++)
            {
                tempChars.Add(ByteToChar(bytes[byteOffset]));
            }

            var tempString = new string(tempChars.ToArray());
            rawValue += tempString;
            return new BencodedString(tempString, rawValue);
        }
    }
}
