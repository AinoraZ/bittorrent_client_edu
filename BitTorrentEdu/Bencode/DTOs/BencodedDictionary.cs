using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
