namespace Bencode.DTOs
{
    public class BencodedInteger : BencodedObject
    {
        public long Value { get; private set; }

        public BencodedInteger(long value, string rawValue) : base(BencodedType.Integer, rawValue)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
