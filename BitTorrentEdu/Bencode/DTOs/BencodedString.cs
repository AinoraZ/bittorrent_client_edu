namespace Bencode.DTOs
{
    public class BencodedString : BencodedObject
    {
        public string Value { get; private set; }

        public BencodedString(string value) : base(BencodedType.String)
        {
            Value = value;
        }
    }
}
