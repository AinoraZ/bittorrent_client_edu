namespace Utils
{
    public interface IByteConverter
    {
        uint BytesToUint(byte[] byteContent);
        byte[] UIntToBytes(uint value);
    }
}