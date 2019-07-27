using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bencode
{
    public static class BencodeConstants
    {
        public const char StringInitial = '0';
        public const char IntegerInitial = 'i';
        public const char ListInitial = 'l';
        public const char DictionaryInitial = 'd';
        public const char EndingCharacter = 'e';
        public const char StringDelimter = ':';
    }
}
