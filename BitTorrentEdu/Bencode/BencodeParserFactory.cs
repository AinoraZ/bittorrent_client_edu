using Bencode.BencodeParsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Bencode
{
    public class BencodeParserFactory : IBencodeParserFactory
    {
        private readonly char[] stringInitials = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        private readonly char[] integerInitials = { BencodeConstants.IntegerInitial };
        private readonly char[] listIntitials = { BencodeConstants.ListInitial };
        private readonly char[] dictionaryIntitials = { BencodeConstants.DictionaryInitial };

        public BencodeParserBase CreateParser(char typeCharacter)
        {
            if (stringInitials.Contains(typeCharacter)) return new StringBencodeParser();
            if (integerInitials.Contains(typeCharacter)) return new IntegerBencodeParser();
            if (listIntitials.Contains(typeCharacter)) return new ListBencodeParser(this);
            if (dictionaryIntitials.Contains(typeCharacter)) return new DictionaryBencodeParser(this);

            throw new Exception("Unsuported type");
        }
    }
}
