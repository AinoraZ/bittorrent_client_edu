using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class ByteConverter : IByteConverter
    {
        public byte[] UIntToBytes(uint value)
        {
            var byteValue = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(byteValue);

            return byteValue;
        }

        public uint BytesToUint(byte[] byteContent)
        {
            if (byteContent.Length != 4)
                throw new ArgumentException("Bytes representing UInt must be of length 4");

            if (BitConverter.IsLittleEndian)
                Array.Reverse(byteContent);

            return BitConverter.ToUInt32(byteContent, 0);
        }
    }
}
