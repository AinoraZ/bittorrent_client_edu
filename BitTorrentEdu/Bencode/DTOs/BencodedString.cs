namespace Bencode.DTOs
{
    public class BencodedString : BencodedObject
    {
        public string Value { get; private set; }

        public BencodedString(string value, string rawValue) : base(BencodedType.String, rawValue)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
