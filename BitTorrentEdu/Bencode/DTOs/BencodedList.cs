using System.Collections.Generic;
using System.Linq;

namespace Bencode.DTOs
{
    public class BencodedList : BencodedObject
    {
        public List<BencodedObject> Value { get; private set; }

        public BencodedList(List<BencodedObject> value, string rawValue) : base(BencodedType.List, rawValue)
        {
            Value = value;
        }

        public override string ToString()
        {
            return string.Join(",", Value.Select(x => x.ToString()).ToArray());
        }
    }
}
