using Bencode.DTOs;

namespace Bencode.BencodeParsers
{
    public class IntegerBencodeParser : BencodeParserBase
    {
        public override BencodedObject Parse(ref byte[] bytes, ref int byteOffset)
        {
            var intValue = ParseIntegerUntilCharacter(ref bytes, ref byteOffset, BencodeConstants.EndingCharacter);
            return new BencodedInteger(intValue, $"{BencodeConstants.IntegerInitial}{intValue}{BencodeConstants.EndingCharacter}");
        }
    }
}
