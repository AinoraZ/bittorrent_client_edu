using System.Collections.Generic;
using Bencode.DTOs;

namespace Bencode
{
    public interface IBencodeParser
    {
        List<BencodedObject> ParseAllBencodeFromFile(string filePath);
    }
}