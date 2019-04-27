using System.Collections.Generic;

namespace Bencode.DTOs
{
    public class BencodedList : BencodedObject
    {
        public List<BencodedObject> Value { get; private set; }

        public BencodedList(List<BencodedObject> value) : base(BencodedType.List)
        {
            Value = value;
        }
    }
}
