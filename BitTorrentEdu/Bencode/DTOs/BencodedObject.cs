namespace Bencode.DTOs
{
    public enum BencodedType
    {
        Integer,
        List,
        Dictionary,
        String
    }

    public class BencodedObject
    {
        public BencodedType Type { get; private set; }
        public string RawValue { get; private set; }

        protected BencodedObject(BencodedType type, string rawValue)
        {
            Type = type;
            RawValue = rawValue;
        }
    }
}
