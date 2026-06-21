using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Veldrith;

/// <summary>
/// Represents the SmallFixedOrDynamicArray data structure used by the graphics runtime.
/// </summary>
internal unsafe struct SmallFixedOrDynamicArray : IDisposable {

    /// <summary>
    /// Stores the max fixed values state used by this instance.
    /// </summary>
    private const int _max_fixed_values = 5;

    /// <summary>
    /// Stores the count value used during command execution.
    /// </summary>
    public readonly uint Count;

    private fixed uint _fixedData[_max_fixed_values];

    /// <summary>
    /// Stores the data state used by this instance.
    /// </summary>
    public readonly uint[] Data;

    /// <summary>
    /// Gets the value value.
    /// </summary>
    /// <param name="i">The i value used by this operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public uint Get(uint i) {
        return this.Count > _max_fixed_values ? this.Data[i] : this._fixedData[i];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SmallFixedOrDynamicArray" /> type.
    /// </summary>
    /// <param name="count">The number of items involved in this operation.</param>
    /// <param name="data">The data value used by this operation.</param>
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
    /// Releases resources held by this instance.
    /// </summary>
    public void Dispose() {
        if (this.Data != null) {
            ArrayPool<uint>.Shared.Return(this.Data);
        }
    }
}