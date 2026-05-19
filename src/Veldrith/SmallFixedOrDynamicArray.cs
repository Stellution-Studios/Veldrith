using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Veldrith
{
    internal unsafe struct SmallFixedOrDynamicArray : IDisposable
    {
        private const int max_fixed_values = 5;

        public readonly uint Count;
        private fixed uint fixedData[max_fixed_values];
        public readonly uint[] Data;

        public uint Get(uint i)
        {
            return Count > max_fixed_values ? Data[i] : fixedData[i];
        }

        public SmallFixedOrDynamicArray(uint count, ref uint data)
        {
            if (count > max_fixed_values)
            {
                Data = ArrayPool<uint>.Shared.Rent((int)count);
                for (int i = 0; i < count; i++)
                {
                    Data[i] = Unsafe.Add(ref data, i);
                }
            }
            else
            {
                for (int i = 0; i < count; i++) fixedData[i] = Unsafe.Add(ref data, i);

                Data = null;
            }

            Count = count;
        }

        public void Dispose()
        {
            if (Data != null) ArrayPool<uint>.Shared.Return(Data);
        }
    }
}
