using System.Collections.Generic;

namespace Bencode.DTOs
{
    public class BencodedList : BencodedObject
    {
        public List<BencodedObject> Value { get; private set; }

        public BencodedList(List<BencodedObject> value, string rawValue) : base(BencodedType.List, rawValue)
        {
            Value = value;
        }
    }
}
