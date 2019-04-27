namespace Bencode.DTOs
{
    public class BencodedInteger : BencodedObject
    {
        public long Value { get; private set; }

        public BencodedInteger(long value) : base(BencodedType.Integer)
        {
            Value = value;
        }
    }
}
