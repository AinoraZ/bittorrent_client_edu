using Bencode.DTOs;

namespace Bencode.BencodeParsers
{
    internal class IntegerBencodeParser : BencodeParserBase
    {
        internal override BencodedObject Parse(ref byte[] bytes, ref int byteOffset)
        {
            var intValue = ParseIntegerUntilCharacter(ref bytes, ref byteOffset, BencodeConstants.EndingCharacter);
            return new BencodedInteger(intValue, $"{BencodeConstants.IntegerInitial}{intValue}{BencodeConstants.EndingCharacter}");
        }
    }
}
