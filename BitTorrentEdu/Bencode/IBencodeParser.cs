using System.Collections.Generic;
using Bencode.DTOs;

namespace Bencode
{
    public interface IBencodeParser
    {
        List<BencodedObject> ParseAllBencodeFromFile(string filePath);
        List<BencodedObject> ParseAllBencodeFromString(string bencodedString);
        List<BencodedObject> ParseAllBencodeFromBytes(ref byte[] bytes);
    }
}