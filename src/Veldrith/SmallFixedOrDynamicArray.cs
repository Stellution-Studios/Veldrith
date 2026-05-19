using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Veldrith;

internal unsafe struct SmallFixedOrDynamicArray : IDisposable {
    private const int _max_fixed_values = 5;

    public readonly uint Count;
    private fixed uint _fixedData[_max_fixed_values];
    public readonly uint[] Data;

    public uint Get(uint i) {
        return this.Count > _max_fixed_values ? this.Data[i] : this._fixedData[i];
    }

    public SmallFixedOrDynamicArray(uint count, ref uint data) {
        if (count > _max_fixed_values) {
            this.Data = ArrayPool<uint>.Shared.Rent((int)count);
            for (int i = 0; i < count; i++) {
                this.Data[i] = Unsafe.Add(ref data, i);
            }
        }
        else {
            for (int i = 0; i < count; i++) {
                this._fixedData[i] = Unsafe.Add(ref data, i);
            }

            this.Data = null;
        }

        this.Count = count;
    }

    public void Dispose() {
        if (this.Data != null) {
            ArrayPool<uint>.Shared.Return(this.Data);
        }
    }
}