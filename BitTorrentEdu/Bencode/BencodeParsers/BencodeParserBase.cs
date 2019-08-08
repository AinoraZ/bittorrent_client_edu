using Bencode.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bencode.BencodeParsers
{
    public abstract class BencodeParserBase
    {
        public abstract BencodedObject Parse(ref byte[] bytes, ref int byteOffset);

        public static char ByteToChar(byte value)
        {
            return Encoding.GetEncoding(28591).GetString(new[] { value })[0];
        }

        public static char GetInitialByte(ref byte[] bytes, ref int byteOffset)
        {
            char initial = ByteToChar(bytes[byteOffset]);
            byteOffset++;

            return initial;
        }

        protected long ParseIntegerUntilCharacter(ref byte[] bytes, ref int byteOffset, char character)
        {
            var tempChars = new List<char>();
            char byteChar;
            for (; (byteChar = ByteToChar(bytes[byteOffset])) != character; byteOffset++)
            {
                tempChars.Add(byteChar);
            }
            byteOffset++;

            var tempString = new string(tempChars.ToArray());
            return long.Parse(tempString);
        }
    }
}
