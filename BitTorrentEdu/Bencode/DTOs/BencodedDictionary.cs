using System.Collections.Generic;
using System.Linq;

namespace Bencode.DTOs
{
    public class BencodedDictionary : BencodedObject
    {
        public Dictionary<string, BencodedObject> Value { get; private set; }

        public BencodedDictionary(Dictionary<string, BencodedObject> value, string rawValue) : base(BencodedType.Dictionary, rawValue)
        {
            Value = value;
        }

        public override string ToString()
        {
            return string.Join(", ", Value.Select(x => $"{x.Key}: {x.Value.ToString()}").ToArray());
        }
    }
}
