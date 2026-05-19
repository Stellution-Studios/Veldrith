using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Veldrith;

/// <summary>
/// Represents the SmallFixedOrDynamicArray struct.
/// </summary>
internal unsafe struct SmallFixedOrDynamicArray : IDisposable {

    /// <summary>
    /// Represents the _max_fixed_values field.
    /// </summary>
    private const int _max_fixed_values = 5;

    /// <summary>
    /// Represents the Count field.
    /// </summary>
    public readonly uint Count;

    private fixed uint _fixedData[_max_fixed_values];

    /// <summary>
    /// Represents the Data field.
    /// </summary>
    public readonly uint[] Data;

    /// <summary>
    /// Performs the Get operation.
    /// </summary>
    /// <param name="i">The value of i.</param>
    /// <returns>The result of the Get operation.</returns>
    public uint Get(uint i) {
        return this.Count > _max_fixed_values ? this.Data[i] : this._fixedData[i];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SmallFixedOrDynamicArray" /> type.
    /// </summary>
    /// <param name="count">The value of count.</param>
    /// <param name="data">The value of data.</param>
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

    /// <summary>
    /// Performs the Dispose operation.
    /// </summary>
    public void Dispose() {
        if (this.Data != null) {
            ArrayPool<uint>.Shared.Return(this.Data);
        }
    }
}