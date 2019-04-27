using System.Collections.Generic;

namespace Bencode.DTOs
{
    public class BencodedDictionary : BencodedObject
    {
        public Dictionary<string, BencodedObject> Value { get; private set; }

        public BencodedDictionary(Dictionary<string, BencodedObject> value) : base(BencodedType.Dictionary)
        {
            Value = value;
        }
    }
}
