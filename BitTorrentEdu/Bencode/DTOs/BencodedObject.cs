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

        protected BencodedObject(BencodedType type)
        {
            Type = type;
        }
    }
}
