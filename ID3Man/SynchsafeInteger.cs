using System;
using System.Linq;

namespace ID3Man
{
    internal class SynchsafeInteger
    {
        private readonly uint _integer;

        public SynchsafeInteger(int integer)
        {
            if ((integer & 0xF0000000) == 0)
            {
                _integer = (uint)integer;
            }
            else
            {
                throw new ArgumentException($"number {integer} is too big");
            }
        }

        public SynchsafeInteger(byte[] array)
        {
            if (array.Length != 4)
            {
                throw new ArgumentException("array must be 4 bytes length");
            }

            var byte1 = new byte[4];
            Array.Copy(array, 0, byte1, 0, 1);
            var i1 = BitConverter.ToUInt32(byte1.Reverse().ToArray());

            var byte2 = new byte[4];
            Array.Copy(array, 1, byte2, 1, 1);
            var i2 = BitConverter.ToUInt32(byte2.Reverse().ToArray());

            var byte3 = new byte[4];
            Array.Copy(array, 2, byte3, 2, 1);
            var i3 = BitConverter.ToUInt32(byte3.Reverse().ToArray());

            var byte4 = new byte[4];
            Array.Copy(array, 3, byte4, 3, 1);
            var i4 = BitConverter.ToUInt32(byte4.Reverse().ToArray());

            var size = i4 + (i3 >> 1) + (i2 >> 2) + (i1 >> 3);
            _integer = size;
        }

        public uint ToUInt()
        {
            return _integer;
        }

        public int ToInt()
        {
            return (int)_integer;
        }

        public byte[] ToArray()
        {
            uint mask1 = 0x0000007F; // b[0]          b[0]         b[0]         b[0111 1111]
            uint mask2 = 0x00003F80; // b[0]          b[0]         b[0011 1111] b[1000 0000]
            uint mask3 = 0x001FC000; // b[0]          b[0001 1111] b[1100 0000] b[0]
            uint mask4 = 0x0FE00000; // b[00000 1111] b[1110 0000] b[0] b[0]

            var frarment1 = _integer & mask1;
            var frarment2 = _integer & mask2;
            var frarment3 = _integer & mask3;
            var frarment4 = _integer & mask4;

            var byte1 = BitConverter.GetBytes(frarment1)[0];
            var byte2 = BitConverter.GetBytes(frarment2 << 1)[1];
            var byte3 = BitConverter.GetBytes(frarment3 << 2)[2];
            var byte4 = BitConverter.GetBytes(frarment4 << 3)[3];

            return new [] {byte4, byte3, byte2, byte1};
        }
    }
}
