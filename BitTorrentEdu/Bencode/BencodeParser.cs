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
        private delegate BencodedObject ParseBencode(ref byte[] bytes, ref int byteOffset);

        private readonly char[] stringInitials = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        private readonly char[] integerInitials = { 'i' };
        private readonly char[] listIntitials = { 'l' };
        private readonly char[] dictionaryIntitials = { 'd' };

        public List<BencodedObject> ParseAllBencodeFromFile(string filePath)
        {
            var returnValue = new List<BencodedObject>();

            int byteOffset = 0;
            var bytes = File.ReadAllBytes(filePath);

            while (byteOffset < bytes.Length)
            {
                var initial = GetInitialByte(ref bytes, ref byteOffset);

                var parser = GetBencodeParser(initial);
                var bencodeObj = parser(ref bytes, ref byteOffset);

                returnValue.Add(bencodeObj);
            }

            return returnValue;
        }

        private BencodedString ParseStringBencode(ref byte[] bytes, ref int byteOffset)
        {
            byteOffset--;
            var strLength = ParseIntegerUntilCharacter(ref bytes, ref byteOffset, ':');
            var offsetLength = byteOffset + strLength;

            var tempChars = new List<char>();
            for (; byteOffset < offsetLength; byteOffset++)
            {
                tempChars.Add(ByteToChar(bytes[byteOffset]));
            }
            byteOffset++;

            return new BencodedString(tempChars.ToString());
        }

        private BencodedInteger ParseIntegerBencode(ref byte[] bytes, ref int byteOffset)
        {
            var intValue = ParseIntegerUntilCharacter(ref bytes, ref byteOffset, 'e');
            return new BencodedInteger(intValue);
        }

        private BencodedList ParseListBencode(ref byte[] bytes, ref int byteOffset)
        {
            var tempValue = new List<BencodedObject>();
            for (; ByteToChar(bytes[byteOffset]) != 'e'; byteOffset++)
            {
                var initial = GetInitialByte(ref bytes, ref byteOffset);
                var parser = GetBencodeParser(initial);

                var bencodedObj = parser(ref bytes, ref byteOffset);

                tempValue.Add(bencodedObj);
            }
            byteOffset++;

            return new BencodedList(tempValue);
        }

        private BencodedDictionary ParseDictionaryBencode(ref byte[] bytes, ref int byteOffset)
        {
            var tempValue = new Dictionary<string, BencodedObject>();
            for (; ByteToChar(bytes[byteOffset]) != 'e'; byteOffset++)
            {
                var bencodedString = ParseStringBencode(ref bytes, ref byteOffset);

                var initial = GetInitialByte(ref bytes, ref byteOffset);
                var parser = GetBencodeParser(initial);

                var bencodedObj = parser(ref bytes, ref byteOffset);

                tempValue.Add(bencodedString.Value, bencodedObj);
            }
            byteOffset++;

            return new BencodedDictionary(tempValue);
        }

        private char GetInitialByte(ref byte[] bytes, ref int byteOffset)
        {
            char initial = ByteToChar(bytes[byteOffset]);
            byteOffset++;

            return initial;
        }

        private ParseBencode GetBencodeParser(char initial)
        {
            if (stringInitials.Contains(initial)) return ParseStringBencode;
            if (integerInitials.Contains(initial)) return ParseIntegerBencode;
            if (listIntitials.Contains(initial)) return ParseListBencode;
            if (dictionaryIntitials.Contains(initial)) return ParseDictionaryBencode;

            throw new Exception("Unsuported type");
        }
        private int ParseIntegerUntilCharacter(ref byte[] bytes, ref int byteOffset, char character)
        {
            var tempChars = new List<char>();
            char byteChar;
            for (; (byteChar = ByteToChar(bytes[byteOffset])) != character; byteOffset++)
            {
                tempChars.Add(byteChar);
            }
            byteOffset++;
            return int.Parse(tempChars.ToString());
        }

        private char ByteToChar(byte value)
        {
            return Encoding.ASCII.GetString(new[] { value })[0];
        }
    }
}
