using Bencode.BencodeParsers;

namespace Bencode
{
    public interface IBencodeParserFactory
    {
        BencodeParserBase CreateParser(char typeCharacter);
    }
}