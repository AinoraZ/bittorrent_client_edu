namespace Bencode.DTOs
{
    public class BencodeInteger : BencodedObject
    {
        public int Value { get; private set; }

        public BencodeInteger(int value) : base(BencodedType.Integer)
        {
            Value = value;
        }
    }
}
